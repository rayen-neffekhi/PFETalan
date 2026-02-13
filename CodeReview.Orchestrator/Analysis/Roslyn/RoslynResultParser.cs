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
        /// TODO: Implement SARIF/JSON parsing.
        /// </summary>
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

                // TODO: Detect SARIF vs custom JSON and map to CodeIssue.
                _logger.LogInformation("Roslyn parser: loaded content; SARIF/JSON mapping not yet implemented.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to parse Roslyn report: {ex.Message}");
            }

            return issues;
        }
    }
}
