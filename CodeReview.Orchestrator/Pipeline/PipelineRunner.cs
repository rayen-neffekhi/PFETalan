using CodeReview.Orchestrator.Analysis;
using CodeReview.Orchestrator.Analysis.Models;
using CodeReview.Orchestrator.Prompting;
using CodeReview.Orchestrator.AI;
using CodeReview.Orchestrator.Feedback;
using CodeReview.Orchestrator.Logging;
using Microsoft.Extensions.Options;
using CodeReview.Orchestrator.Configuration;

namespace CodeReview.Orchestrator.Pipeline
{
    /// <summary>
    /// Orchestrates the internal processing pipeline: parsing, aggregation, prompting, LLM calls, feedback.
    /// </summary>
    public class PipelineRunner
    {
        private readonly PipelineContext _context;
        private readonly Analysis.Sonar.SonarResultParser _sonarParser;
        private readonly Analysis.Roslyn.RoslynResultParser _roslynParser;
        private readonly Analysis.IssueAggregator _aggregator;
        private readonly PromptBuilder _promptBuilder;
        private readonly ILLMClient _llmClient;
        private readonly PullRequestCommenter _commenter;
        private readonly ReviewSummaryBuilder _summaryBuilder;
        private readonly PipelineLogger _logger;

        /// <summary>
        /// Create a new PipelineRunner.
        /// </summary>
        public PipelineRunner(
            PipelineContext context,
            Analysis.Sonar.SonarResultParser sonarParser,
            Analysis.Roslyn.RoslynResultParser roslynParser,
            Analysis.IssueAggregator aggregator,
            PromptBuilder promptBuilder,
            ILLMClient llmClient,
            PullRequestCommenter commenter,
            ReviewSummaryBuilder summaryBuilder,
            PipelineLogger logger)
        {
            _context = context;
            _sonarParser = sonarParser;
            _roslynParser = roslynParser;
            _aggregator = aggregator;
            _promptBuilder = promptBuilder;
            _llmClient = llmClient;
            _commenter = commenter;
            _summaryBuilder = summaryBuilder;
            _logger = logger;
        }

        /// <summary>
        /// Run the pipeline end-to-end.
        /// </summary>
        public async Task RunAsync()
        {
            _logger.LogInformation($"Pipeline run id: {_context.CorrelationId}");

            // Load issues
            var sonarIssues = await _sonarParser.ParseAsync(_context.Settings.SonarReportPath);
            var roslynIssues = await _roslynParser.ParseAsync(_context.Settings.RoslynReportPath);

            _logger.LogInformation($"Parsed {sonarIssues.Count} Sonar issues and {roslynIssues.Count} Roslyn issues.");

            // Aggregate
            var allIssues = _aggregator.Aggregate(sonarIssues, roslynIssues);
            _logger.LogInformation($"Aggregated and normalized {allIssues.Count} issues.");

            // Filter by severity
            var relevant = _aggregator.FilterBySeverity(allIssues, "Major");
            _logger.LogInformation($"{relevant.Count} issues exceed severity threshold and are considered relevant.");

            string llmResponse = string.Empty;

            if (relevant.Count > 0 && _context.Settings.LlmEnabled)
            {
                var prompt = await _promptBuilder.BuildPromptAsync(relevant, _context);
                _logger.LogInformation("Sending prompt to LLM (abstracted client)." );
                try
                {
                    llmResponse = await _llmClient.SendPromptAsync(prompt, _context.Settings.Gemini);
                    _logger.LogInformation("Received response from LLM.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"LLM call failed: {ex.Message}. Continuing without LLM.");
                }
            }
            else
            {
                _logger.LogInformation("No relevant issues or LLM disabled - skipping LLM call.");
            }

            // Build feedback artifacts
            var summary = _summaryBuilder.BuildSummary(allIssues, llmResponse);

            // Publish - these are conceptual/stubbed implementations (TODO: integrate with GitHub API)
            await _commenter.PublishInlineCommentsAsync(allIssues, llmResponse);
            await _commenter.PublishSummaryAsync(summary);

            _logger.LogInformation("Feedback publishing completed (conceptual/stubbed)." );
        }
    }
}
