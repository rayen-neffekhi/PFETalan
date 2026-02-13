using System;

namespace CodeReview.Orchestrator.Analysis.Models
{
    /// <summary>
    /// Represents a normalized code analysis issue from different tools.
    /// </summary>
    public class CodeIssue
    {
        /// <summary>
        /// Tool that reported the issue (SonarQube, Roslyn, ...)
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Unique identifier for the issue within the source.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Issue severity: Critical, Major, Minor, Info.
        /// </summary>
        public string Severity { get; set; } = "Info";

        /// <summary>
        /// Short message or title for the issue.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// File path related to the issue (relative to repo root in CI).
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Optional line number for inline comments.
        /// </summary>
        public int? Line { get; set; }

        /// <summary>
        /// Optional link to the original report entry.
        /// </summary>
        public string? Url { get; set; }
    }
}
