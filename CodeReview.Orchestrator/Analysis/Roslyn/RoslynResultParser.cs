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

                // SARIF format: root.runs[].results
                JsonElement root = doc.RootElement;

                if (!root.TryGetProperty("runs", out var runs) || runs.ValueKind != JsonValueKind.Array)
                {
                    _logger.LogWarning("Roslyn parser: 'runs' array not found in SARIF report.");
                    return issues;
                }

                foreach (var run in runs.EnumerateArray())
                {
                    if (!run.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
                        continue;

                    foreach (var r in results.EnumerateArray())
                    {
                        string id = r.GetProperty("ruleId").GetString() ?? string.Empty;
                        string message = r.GetProperty("message").GetProperty("text").GetString() ?? string.Empty;
                        string severity = r.TryGetProperty("level", out var levelProp) ? levelProp.GetString() ?? "Warning" : "Warning";

                        string? filePath = null;
                        int? line = null;

                        if (r.TryGetProperty("locations", out var locations) && locations.ValueKind == JsonValueKind.Array)
                        {
                            var firstLoc = locations[0];
                            if (firstLoc.TryGetProperty("resultFile", out var fileProp))
                            {
                                filePath = fileProp.GetProperty("uri").GetString()?.Replace("file:///", "");
                                if (fileProp.TryGetProperty("region", out var region) && region.TryGetProperty("startLine", out var startLine))
                                    line = startLine.GetInt32();
                            }
                        }

                        issues.Add(new CodeIssue
                        {
                            Source = "Roslyn",
                            Id = id,
                            Severity = severity,
                            Message = message,
                            FilePath = filePath,
                            Line = line
                        });
                    }
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