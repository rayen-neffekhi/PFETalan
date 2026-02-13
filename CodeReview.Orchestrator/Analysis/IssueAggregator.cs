using CodeReview.Orchestrator.Analysis.Models;

namespace CodeReview.Orchestrator.Analysis
{
    /// <summary>
    /// Aggregates and filters issues from multiple analysis tools.
    /// </summary>
    public class IssueAggregator
    {
        /// <summary>
        /// Aggregate lists from multiple parsers and normalize duplicates.
        /// </summary>
        public List<CodeIssue> Aggregate(IEnumerable<CodeIssue> sonar, IEnumerable<CodeIssue> roslyn)
        {
            var list = new List<CodeIssue>();
            if (sonar != null) list.AddRange(sonar);
            if (roslyn != null) list.AddRange(roslyn);

            // TODO: Implement deduplication and normalization logic.
            return list;
        }

        /// <summary>
        /// Filter by severity. Accepts threshold as string (Critical, Major, Minor, Info).
        /// </summary>
        public List<CodeIssue> FilterBySeverity(IEnumerable<CodeIssue> issues, string threshold)
        {
            var levels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Critical"] = 4,
                ["Major"] = 3,
                ["Minor"] = 2,
                ["Info"] = 1
            };

            if (!levels.TryGetValue(threshold ?? "Major", out var minLevel))
            {
                minLevel = 3; // default Major
            }

            return issues.Where(i => levels.TryGetValue(i.Severity ?? "Info", out var lvl) && lvl >= minLevel).ToList();
        }
    }
}
