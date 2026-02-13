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

                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    // Root is an array of issues (custom Roslyn JSON)
                    foreach (var result in doc.RootElement.EnumerateArray())
                    {
                        issues.Add(MapCustomRoslynIssue(result));
                    }

                    _logger.LogInformation($"Roslyn parser: mapped {issues.Count} issues from root array.");
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    // SARIF format
                    if (doc.RootElement.TryGetProperty("runs", out var runs) && runs.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var run in runs.EnumerateArray())
                        {
                            if (!run.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
                                continue;

                            foreach (var result in results.EnumerateArray())
                                issues.Add(MapSarifIssue(result));
                        }

                        _logger.LogInformation($"Roslyn parser: mapped {issues.Count} issues from SARIF object.");
                    }
                    // Custom JSON under "issues" property
                    else if (doc.RootElement.TryGetProperty("issues", out var customIssues) && customIssues.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var result in customIssues.EnumerateArray())
                            issues.Add(MapCustomRoslynIssue(result));

                        _logger.LogInformation($"Roslyn parser: mapped {issues.Count} issues from 'issues' property.");
                    }
                    else
                    {
                        _logger.LogWarning("Roslyn parser: unrecognized object structure.");
                    }
                }
                else
                {
                    _logger.LogWarning("Roslyn parser: root JSON is neither object nor array.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to parse Roslyn report: {ex.Message}");
            }

            return issues;
        }

        private CodeIssue MapSarifIssue(JsonElement result)
        {
            string message = string.Empty;
            if (result.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.Object)
                message = msgProp.TryGetProperty("text", out var txtProp) ? txtProp.GetString() ?? string.Empty : string.Empty;

            int? line = null;
            string filePath = string.Empty;

            if (result.TryGetProperty("locations", out var locArray) && locArray.ValueKind == JsonValueKind.Array && locArray.GetArrayLength() > 0)
            {
                var loc = locArray[0];
                if (loc.TryGetProperty("physicalLocation", out var phys) && phys.ValueKind == JsonValueKind.Object)
                {
                    if (phys.TryGetProperty("artifactLocation", out var artifact) &&
                        artifact.TryGetProperty("uri", out var uriProp))
                        filePath = uriProp.GetString() ?? string.Empty;

                    if (phys.TryGetProperty("region", out var region) &&
                        region.TryGetProperty("startLine", out var startLine))
                        line = startLine.GetInt32();
                }
            }

            return new CodeIssue
            {
                Source = "Roslyn",
                Id = result.TryGetProperty("ruleId", out var rid) ? rid.GetString() ?? string.Empty : string.Empty,
                Severity = result.TryGetProperty("level", out var lvl) ? lvl.GetString() ?? "Info" : "Info",
                Message = message,
                FilePath = filePath,
                Line = line
            };
        }

        private CodeIssue MapCustomRoslynIssue(JsonElement result)
        {
            string message = string.Empty;
            if (result.TryGetProperty("message", out var msgProp))
            {
                if (msgProp.ValueKind == JsonValueKind.Object && msgProp.TryGetProperty("text", out var txtProp))
                    message = txtProp.GetString() ?? string.Empty;
                else if (msgProp.ValueKind == JsonValueKind.String)
                    message = msgProp.GetString() ?? string.Empty;
            }

            int? line = result.TryGetProperty("line", out var lineProp) && lineProp.ValueKind == JsonValueKind.Number ? lineProp.GetInt32() : (int?)null;

            return new CodeIssue
            {
                Source = "Roslyn",
                Id = result.TryGetProperty("ruleId", out var rid) ? rid.GetString() ?? string.Empty : string.Empty,
                Severity = result.TryGetProperty("level", out var lvl) ? lvl.GetString() ?? "Info" : "Info",
                Message = message,
                FilePath = result.TryGetProperty("filePath", out var fp) ? fp.GetString() ?? string.Empty : string.Empty,
                Line = line
            };
        }
    }
}
