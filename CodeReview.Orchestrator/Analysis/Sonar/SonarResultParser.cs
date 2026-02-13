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
                    i.TryGetProperty("key", out var keyProp);
                    i.TryGetProperty("severity", out var sevProp);
                    i.TryGetProperty("message", out var msgProp);
                    i.TryGetProperty("component", out var compProp);
                    i.TryGetProperty("line", out var lineProp);
                    i.TryGetProperty("project", out var projectProp);
                    i.TryGetProperty("externalRuleEngine", out var engineProp);

                    string projectKey = projectProp.GetString() ?? string.Empty;

                    issues.Add(new CodeIssue
                    {
                        Source = engineProp.GetString() ?? "SonarCloud",
                        Id = keyProp.GetString() ?? string.Empty,
                        Severity = sevProp.GetString() ?? "Info",
                        Message = msgProp.GetString() ?? string.Empty,
                        FilePath = compProp.GetString() ?? string.Empty,
                        Line = lineProp.ValueKind == JsonValueKind.Number ? lineProp.GetInt32() : (int?)null,
                        Url = !string.IsNullOrEmpty(projectKey) ? $"https://sonarcloud.io/project/issues?id={projectKey}" : null
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
