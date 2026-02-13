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

                using var doc = JsonDocument.Parse(content);

                // SARIF format
                if (doc.RootElement.TryGetProperty("runs", out var runs))
                {
                    foreach (var run in runs.EnumerateArray())
                    {
                        if (!run.TryGetProperty("results", out var results))
                            continue;

                        foreach (var result in results.EnumerateArray())
                        {
                            var location = result.GetProperty("locations")[0].GetProperty("physicalLocation");

                            int? line = null;
                            if (location.TryGetProperty("region", out var region) &&
                                region.TryGetProperty("startLine", out var startLine))
                                line = startLine.GetInt32();

                            string filePath = string.Empty;
                            if (location.TryGetProperty("artifactLocation", out var artifact) &&
                                artifact.TryGetProperty("uri", out var uri))
                                filePath = uri.GetString() ?? string.Empty;

                            issues.Add(new CodeIssue
                            {
                                Source = "Roslyn",
                                Id = result.GetProperty("ruleId").GetString() ?? string.Empty,
                                Severity = result.GetProperty("level").GetString() ?? "Info",
                                Message = result.GetProperty("message").GetProperty("text").GetString() ?? string.Empty,
                                FilePath = filePath,
                                Line = line
                            });
                        }
                    }
                }
                // Custom Roslyn JSON format
                else if (doc.RootElement.TryGetProperty("issues", out var customIssues))
                {
                    foreach (var i in customIssues.EnumerateArray())
                    {
                        issues.Add(new CodeIssue
                        {
                            Source = "Roslyn",
                            Id = i.GetProperty("ruleId").GetString() ?? string.Empty,
                            Severity = i.GetProperty("level").GetString() ?? "Info",
                            Message = i.GetProperty("message").GetProperty("text").GetString() ?? string.Empty,
                            FilePath = i.GetProperty("filePath").GetString() ?? string.Empty,
                            Line = i.TryGetProperty("line", out var lineProp) ? lineProp.GetInt32() : (int?)null
                        });
                    }
                }
                else
                {
                    _logger.LogWarning("Roslyn parser: no recognizable SARIF or JSON structure found.");
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

