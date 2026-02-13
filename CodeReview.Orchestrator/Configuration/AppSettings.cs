using System.Collections.Generic;

namespace CodeReview.Orchestrator.Configuration
{
    /// <summary>
    /// Application configuration settings loaded from configuration providers.
    /// Update values in `appsettings.json` or CI secrets as appropriate.
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Path to SonarQube JSON report. TODO: Set by CI step.
        /// </summary>
        public string SonarReportPath { get; set; } = "reports/sonar-report.json";

        /// <summary>
        /// Path to Roslyn analyzers report (JSON or SARIF). TODO: Set by CI step.
        /// </summary>
        public string RoslynReportPath { get; set; } = "reports/roslyn-report.json";

        /// <summary>
        /// Whether to call the LLM. In CI, control via pipeline variables.
        /// </summary>
        public bool LlmEnabled { get; set; } = true;

        /// <summary>
        /// Severity threshold to send to LLM. Accepts: Critical, Major, Minor, Info.
        /// </summary>
        public string SeverityThreshold { get; set; } = "Major";

        /// <summary>
        /// Google Gemini API configuration - DO NOT STORE SECRETS IN SOURCE CONTROL.
        /// Provide values via pipeline secrets or environment variables.
        /// </summary>
        public GeminiSettings Gemini { get; set; } = new GeminiSettings();

        /// <summary>
        /// GitHub context (placeholder) - use environment variables in CI to set these.
        /// </summary>
        public IDictionary<string, string> GitHubContext { get; set; } = new Dictionary<string, string>();
    }
}