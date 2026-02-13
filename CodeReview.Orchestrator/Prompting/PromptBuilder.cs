using CodeReview.Orchestrator.Analysis.Models;
using CodeReview.Orchestrator.Pipeline;
using CodeReview.Orchestrator.Utils;
using CodeReview.Orchestrator.Logging;

namespace CodeReview.Orchestrator.Prompting
{
    /// <summary>
    /// Builds prompts for the LLM using external templates.
    /// </summary>
    public class PromptBuilder
    {
        private readonly FileLoader _loader;
        private readonly PipelineLogger _logger;

        public PromptBuilder(FileLoader loader, PipelineLogger logger)
        {
            _loader = loader;
            _logger = logger;
        }

        /// <summary>
        /// Build a prompt using the stored template and the given issues.
        /// The template file should contain a marker like {{ISSUES}} where the issues text will be injected.
        /// </summary>
        public async Task<string> BuildPromptAsync(IEnumerable<CodeIssue> issues, PipelineContext context)
        {
            var tpl = await _loader.LoadTextAsync("Prompting/PromptTemplates/CodeReviewPrompt.txt");
            if (string.IsNullOrWhiteSpace(tpl))
            {
                _logger.LogWarning("Prompt template not found; using fallback simple prompt.");
                tpl = "Please review the following issues:\n{{ISSUES}}";
            }

            var sb = new System.Text.StringBuilder();
            foreach (var i in issues)
            {
                sb.AppendLine($"- [{i.Severity}] {i.FilePath}:{i.Line ?? 0} - {i.Message} (Source: {i.Source})");
            }

            var issuesText = sb.ToString();
            var prompt = tpl.Replace("{{ISSUES}}", issuesText);

            // Add contextual information
            prompt += $"\n\nRunId: {context.CorrelationId}\n";
            return prompt;
        }
    }
}
