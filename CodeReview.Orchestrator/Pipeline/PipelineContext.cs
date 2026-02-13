using CodeReview.Orchestrator.Configuration;

namespace CodeReview.Orchestrator.Pipeline
{
    /// <summary>
    /// Context object shared across pipeline stages.
    /// </summary>
    public class PipelineContext
    {
        /// <summary>
        /// Application settings provided at startup.
        /// </summary>
        public AppSettings Settings { get; set; } = new AppSettings();

        /// <summary>
        /// Correlation id for this run.
        /// </summary>
        public string CorrelationId { get; set; } = System.Guid.NewGuid().ToString();
    }
}
