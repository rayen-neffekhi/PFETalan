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

                JsonElement root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("issues", out var issueArray))
                    root = issueArray;

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

                    issues.Add(new CodeIssue
                    {
                        Source = "Roslyn",
                        Id = key,
                        Severity = severity,
                        Message = message,
                        FilePath = component,
                        Line = line
                    });
                }

                _logger.LogInformation($"Roslyn parser: mapped {issues.Count} issues from root array/object.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to parse Roslyn report: {ex.Message}");
            }

            return issues;
        }
    }
}