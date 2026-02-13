using System.Text.Json;
using CodeReview.Orchestrator.Analysis.Models;
using CodeReview.Orchestrator.Logging;
using CodeReview.Orchestrator.Utils;

namespace CodeReview.Orchestrator.Analysis.Roslyn
{
    /// <summary>
    /// Parses Roslyn analyzer output (JSON or SARIF) into normalized <see cref="CodeIssue"/> objects.
    /// </summary>
    public class RoslynResultParser
    {
        private readonly PipelineLogger _logger;
        private readonly FileLoader _loader;

        public RoslynResultParser(PipelineLogger logger, FileLoader loader)
        {
            _logger = logger;
            _loader = loader;
        }
              /// <summary>
        /// Parse Roslyn analyzer results at path into a list of CodeIssue.
        /// </summary>
        public async Task<List<CodeIssue>> ParseAsync(string path)
        {
            var issues = new List<CodeIssue>();

            try
            {
                var json = await _loader.LoadTextAsync(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogWarning($"Roslyn report not found at {path}. Returning empty list.");
                    return issues;
                }

                using var doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                // --- SARIF format handling ---
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("runs", out var runs) && runs.ValueKind == JsonValueKind.Array)
                {
                    foreach (var run in runs.EnumerateArray())
                    {
                        if (!run.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
                            continue;

                        foreach (var result in results.EnumerateArray())
                        {
                            string key = result.TryGetProperty("ruleId", out var ruleProp) ? ruleProp.GetString() ?? "" : "";
                            string severity = result.TryGetProperty("level", out var levelProp) ? levelProp.GetString() ?? "Info" : "Info";

                            string message = "";
                            if (result.TryGetProperty("message", out var msgProp) && msgProp.TryGetProperty("text", out var textProp))
                                message = textProp.GetString() ?? "";

                            string filePath = "";
                            int? line = null;

                            if (result.TryGetProperty("locations", out var locs) && locs.ValueKind == JsonValueKind.Array && locs.GetArrayLength() > 0)
                            {
                                var firstLoc = locs[0];
                                if (firstLoc.TryGetProperty("resultFile", out var fileProp))
                                {
                                    filePath = fileProp.GetProperty("uri").GetString() ?? "";
                                    filePath = filePath.Replace("file:///", ""); // clean up URI
                                    if (fileProp.TryGetProperty("region", out var region) && region.TryGetProperty("startLine", out var lineProp))
                                        line = lineProp.GetInt32();
                                }
                            }

                            issues.Add(new CodeIssue
                            {
                                Source = "Roslyn",
                                Id = key,
                                Severity = severity,
                                Message = message,
                                FilePath = filePath,
                                Line = line
                            });
                        }
                    }

                    _logger.LogInformation($"Roslyn parser (SARIF): mapped {issues.Count} issues to CodeIssue.");
                    return issues;
                }

                // --- Fallback: old "issues" array (Sonar-like JSON) ---
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("issues", out var array))
                    root = array;

                if (root.ValueKind != JsonValueKind.Array)
                {
                    _logger.LogWarning("Roslyn parser: root element is not an array.");
                    return issues;
                }

                foreach (var i in root.EnumerateArray())
                {
                    string key = i.TryGetProperty("key", out var keyProp) && keyProp.ValueKind == JsonValueKind.String
                        ? keyProp.GetString() ?? string.Empty
                        : string.Empty;

                    string severity = i.TryGetProperty("severity", out var sevProp) && sevProp.ValueKind == JsonValueKind.String
                        ? sevProp.GetString() ?? "Info"
                        : "Info";

                    string message = i.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String
                        ? msgProp.GetString() ?? string.Empty
                        : string.Empty;

                    string component = i.TryGetProperty("component", out var compProp) && compProp.ValueKind == JsonValueKind.String
                        ? compProp.GetString() ?? string.Empty
                        : string.Empty;

                    int? line = null;
                    if (i.TryGetProperty("line", out var lineProp) && lineProp.ValueKind == JsonValueKind.Number)
                        line = lineProp.GetInt32();

                    string source = i.TryGetProperty("externalRuleEngine", out var engineProp) && engineProp.ValueKind == JsonValueKind.String
                        ? engineProp.GetString() ?? "Roslyn"
                        : "Roslyn";

                    string? projectKey = i.TryGetProperty("project", out var projProp) && projProp.ValueKind == JsonValueKind.String
                        ? projProp.GetString()
                        : null;

                    string? url = !string.IsNullOrEmpty(projectKey)
                        ? $"https://sonarcloud.io/project/issues?id={projectKey}&search={key}"
                        : null;

                    issues.Add(new CodeIssue
                    {
                        Source = source,
                        Id = key,
                        Severity = severity,
                        Message = message,
                        FilePath = component,
                        Line = line,
                        Url = url
                    });
                }

                _logger.LogInformation($"Roslyn parser: mapped {issues.Count} issues to CodeIssue.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to parse Roslyn report: {ex.Message}");
            }

            return issues;
        }
    }
}