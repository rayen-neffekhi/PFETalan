namespace CodeReview.Orchestrator.Configuration
{
    /// <summary>
    /// Google Gemini API configuration settings.
    /// Secrets must come from CI secrets / environment variables.
    /// DO NOT STORE SECRETS IN SOURCE CONTROL.
    /// </summary>
    public class GeminiSettings : LLMSettings
    {
        /// <summary>
        /// The Google Gemini API endpoint URL.
        /// Default points to Gemini 1.5 Flash model.
        /// </summary>
        public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent";

        /// <summary>
        /// Environment variable name for the Gemini API key.
        /// Default: GOOGLE_GEMINI_API_KEY
        /// </summary>
        public new string ApiKeyEnvVarName { get; set; } = "GEMINI_API_KEY";
    }
}
