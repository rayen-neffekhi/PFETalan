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
                var root = doc.RootElement;

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
                        string id = r.TryGetProperty("ruleId", out var idProp) && idProp.ValueKind == JsonValueKind.String
                            ? idProp.GetString() ?? string.Empty
                            : string.Empty;

                        string message = string.Empty;
                            if (r.TryGetProperty("message", out var msgProp))
                            {
                                if (msgProp.ValueKind == JsonValueKind.Object && msgProp.TryGetProperty("text", out var textProp))
                                    message = textProp.GetString() ?? string.Empty;
                                else if (msgProp.ValueKind == JsonValueKind.String)
                                    message = msgProp.GetString() ?? string.Empty;
        }
                        string severity = r.TryGetProperty("level", out var levelProp) && levelProp.ValueKind == JsonValueKind.String
                            ? levelProp.GetString() ?? "Warning"
                            : "Warning";

                        string? filePath = null;
                        int? line = null;

                        if (r.TryGetProperty("locations", out var locations) && locations.ValueKind == JsonValueKind.Array && locations.GetArrayLength() > 0)
                        {
                            var firstLoc = locations[0];
                            if (firstLoc.TryGetProperty("resultFile", out var fileProp))
                            {
                                filePath = fileProp.TryGetProperty("uri", out var uriProp) && uriProp.ValueKind == JsonValueKind.String
                                    ? uriProp.GetString()?.Replace("file:///", "")
                                    : null;

                                if (fileProp.TryGetProperty("region", out var region) &&
                                    region.TryGetProperty("startLine", out var startLine) &&
                                    startLine.ValueKind == JsonValueKind.Number)
                                {
                                    line = startLine.GetInt32();
                                }
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

                _logger.LogInformation($"Roslyn parser: mapped {issues.Count} issues from SARIF.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to parse Roslyn report: {ex.Message}");
            }

            return issues;
        }
    }
}