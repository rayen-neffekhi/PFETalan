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
                var content = await _loader.LoadTextAsync(path);
                if (string.IsNullOrWhiteSpace(content))
                {
                    _logger.LogWarning($"Roslyn report not found at {path}. Returning empty list.");
                    return issues;
                }

                using var doc = JsonDocument.Parse(content);

                // SARIF format
                if (doc.RootElement.TryGetProperty("runs", out var runs) && runs.ValueKind == JsonValueKind.Array)
                {
                    foreach (var run in runs.EnumerateArray())
                    {
                        if (!run.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
                            continue;

                        foreach (var result in results.EnumerateArray())
                        {
                            int? line = null;
                            string filePath = string.Empty;
                            string message = string.Empty;

                            // Location
                            if (result.TryGetProperty("locations", out var locArray) && locArray.ValueKind == JsonValueKind.Array && locArray.GetArrayLength() > 0)
                            {
                                var loc = locArray[0];
                                if (loc.TryGetProperty("physicalLocation", out var phys) && phys.ValueKind == JsonValueKind.Object)
                                {
                                    if (phys.TryGetProperty("artifactLocation", out var artifact) &&
                                        artifact.TryGetProperty("uri", out var uriProp))
                                    {
                                        filePath = uriProp.GetString() ?? string.Empty;
                                    }

                                    if (phys.TryGetProperty("region", out var region) &&
                                        region.TryGetProperty("startLine", out var startLine))
                                    {
                                        line = startLine.GetInt32();
                                    }
                                }
                            }

                            // Message
                            if (result.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.Object)
                                message = msgProp.TryGetProperty("text", out var textProp) ? textProp.GetString() ?? string.Empty : string.Empty;

                            issues.Add(new CodeIssue
                            {
                                Source = "Roslyn",
                                Id = result.TryGetProperty("ruleId", out var ruleId) ? ruleId.GetString() ?? string.Empty : string.Empty,
                                Severity = result.TryGetProperty("level", out var level) ? level.GetString() ?? "Info" : "Info",
                                Message = message,
                                FilePath = filePath,
                                Line = line
                            });
                        }
                    }

                    _logger.LogInformation($"Roslyn parser: mapped {issues.Count} issues to CodeIssue (SARIF).");
                }
                // Custom Roslyn JSON format
                else if (doc.RootElement.TryGetProperty("issues", out var customIssues) && customIssues.ValueKind == JsonValueKind.Array)
                {
                    foreach (var i in customIssues.EnumerateArray())
                    {
                        issues.Add(new CodeIssue
                        {
                            Source = "Roslyn",
                            Id = i.TryGetProperty("ruleId", out var rid) ? rid.GetString() ?? string.Empty : string.Empty,
                            Severity = i.TryGetProperty("level", out var lvl) ? lvl.GetString() ?? "Info" : "Info",
                            Message = i.TryGetProperty("message", out var msgObj) && msgObj.ValueKind == JsonValueKind.Object && msgObj.TryGetProperty("text", out var txtProp) ? txtProp.GetString() ?? string.Empty : string.Empty,
                            FilePath = i.TryGetProperty("filePath", out var fp) ? fp.GetString() ?? string.Empty : string.Empty,
                            Line = i.TryGetProperty("line", out var lineProp) ? lineProp.GetInt32() : (int?)null
                        });
                    }

                    _logger.LogInformation($"Roslyn parser: mapped {issues.Count} issues to CodeIssue (custom JSON).");
                }
                else
                {
                    _logger.LogWarning("Roslyn parser: no recognizable SARIF or custom JSON structure found.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to parse Roslyn report: {ex.Message}");
            }

            return issues;
        }
    }
}

