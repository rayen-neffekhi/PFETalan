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

        /// <summary>
        /// Parse Sonar JSON at path into a list of CodeIssue.
        /// TODO: Implement actual Sonar JSON schema mapping. Currently returns empty list when missing.
        /// </summary>
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

                // TODO: Deserialize according to Sonar report JSON schema.
                using var doc = JsonDocument.Parse(json);
                // Example: map doc elements into CodeIssue objects.

                _logger.LogInformation("Sonar parser: parsed JSON; mapping to CodeIssue not yet implemented.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to parse Sonar report: {ex.Message}");
            }

            return issues;
        }
    }
}
