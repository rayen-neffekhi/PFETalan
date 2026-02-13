using System.Text.Json;
using CodeReview.Orchestrator.Analysis.Models;
using CodeReview.Orchestrator.Logging;
using CodeReview.Orchestrator.Utils;

namespace CodeReview.Orchestrator.Analysis.Sonar
{
    /// <summary>
    /// Parses SonarQube JSON reports into normalized <see cref="CodeIssue"/> objects.
    /// </summary>
    public class SonarResultParser
    {
        private readonly PipelineLogger _logger;
        private readonly FileLoader _loader;

        public SonarResultParser(PipelineLogger logger, FileLoader loader)
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
                    _logger.LogWarning($"Sonar report not found at {path}. Returning empty list.");
                    return issues;
                }

                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("issues", out var issuesArray) || issuesArray.ValueKind != JsonValueKind.Array)
                {
                    _logger.LogWarning("Sonar parser: no 'issues' array found in JSON.");
                    return issues;
                }

                foreach (var i in issuesArray.EnumerateArray())
                {
                    string source = i.TryGetProperty("externalRuleEngine", out var engineProp) && engineProp.ValueKind == JsonValueKind.String
                        ? engineProp.GetString() ?? "SonarCloud"
                        : "SonarCloud";

                    // Skip Roslyn issues in Sonar parser
                    if (string.Equals(source, "Roslyn", StringComparison.OrdinalIgnoreCase))
                        continue;

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

                    string? projectKey = i.TryGetProperty("project", out var projProp) && projProp.ValueKind == JsonValueKind.String
                        ? projProp.GetString()
                        : null;

                    string? url = !string.IsNullOrEmpty(projectKey) ? $"https://sonarcloud.io/project/issues?id={projectKey}" : null;

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

                _logger.LogInformation($"Sonar parser: mapped {issues.Count} issues to CodeIssue.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to parse Sonar report: {ex.Message}");
            }

            return issues;
        }
    }
}