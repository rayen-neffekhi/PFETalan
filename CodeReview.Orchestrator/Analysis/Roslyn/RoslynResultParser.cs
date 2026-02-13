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
        /// Supports both root-array JSON and object-with-issues-array JSON.
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

                JsonElement issuesArray;

                // Handle root array
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    issuesArray = doc.RootElement;
                }
                // Handle root object with "issues" property
                else if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                         doc.RootElement.TryGetProperty("issues", out var tmpArray) &&
                         tmpArray.ValueKind == JsonValueKind.Array)
                {
                    issuesArray = tmpArray;
                }
                else
                {
                    _logger.LogWarning("Roslyn parser: unexpected JSON format, no issues found.");
                    return issues;
                }

                foreach (var i in issuesArray.EnumerateArray())
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

                    issues.Add(new CodeIssue
                    {
                        Source = source,
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