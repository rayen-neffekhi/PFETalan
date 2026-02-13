using CodeReview.Orchestrator.Analysis.Models;
using CodeReview.Orchestrator.Logging;

namespace CodeReview.Orchestrator.Feedback
{
    /// <summary>
    /// Publishes inline PR comments and a global summary.
    /// Currently conceptual/stubbed - integrate with GitHub API in CI.
    /// </summary>
    public class PullRequestCommenter
    {
        private readonly PipelineLogger _logger;

        public PullRequestCommenter(PipelineLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Publish inline comments to the Pull Request for issues with file/line context.
        /// TODO: Implement GitHub REST or GraphQL calls using GH token from CI secrets.
        /// </summary>
        public Task PublishInlineCommentsAsync(IEnumerable<CodeIssue> issues, string llmResponse)
        {
            _logger.LogInformation("Publishing inline comments (stub).");
            foreach (var issue in issues)
            {
                // This is a stub. Replace with calls to `POST /repos/{owner}/{repo}/pulls/{pull_number}/comments`.
                _logger.LogInformation($"[Inline] {issue.FilePath}:{issue.Line} [{issue.Severity}] {issue.Message}");
            }

            // Optionally parse llmResponse for per-issue comments.
            return Task.CompletedTask;
        }

        /// <summary>
        /// Publish a global review summary to the Pull Request (conceptual/stubbed).
        /// </summary>
        public Task PublishSummaryAsync(string summary)
        {
            _logger.LogInformation("Publishing PR summary (stub):");
            _logger.LogInformation(summary ?? "(empty)");

            // TODO: Use GitHub Checks API or PR comment to publish the summary.
            return Task.CompletedTask;
        }
    }
}
