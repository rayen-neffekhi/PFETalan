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

                if (!doc.RootElement.TryGetProperty("issues", out var issuesArray))
                {
                    _logger.LogWarning("Sonar parser: no 'issues' array found in JSON.");
                    return issues;
                }

                foreach (var i in issuesArray.EnumerateArray())
                {
                    issues.Add(new CodeIssue
                    {
                        Source = i.GetProperty("externalRuleEngine").GetString() ?? "SonarCloud",
                        Id = i.GetProperty("key").GetString() ?? string.Empty,
                        Severity = i.GetProperty("severity").GetString() ?? "Info",
                        Message = i.GetProperty("message").GetString() ?? string.Empty,
                        FilePath = i.GetProperty("component").GetString() ?? string.Empty,
                        Line = i.TryGetProperty("line", out var lineProp) ? lineProp.GetInt32() : (int?)null,
                        Url = $"https://sonarcloud.io/project/issues?id={i.GetProperty("project").GetString()}"
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
