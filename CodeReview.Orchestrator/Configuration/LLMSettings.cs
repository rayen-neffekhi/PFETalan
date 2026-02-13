namespace CodeReview.Orchestrator.Configuration
{
    /// <summary>
    /// Base class for LLM provider settings.
    /// Implement this for different LLM providers (Azure, Gemini, OpenAI, etc.).
    /// </summary>
    public abstract class LLMSettings
    {
        /// <summary>
        /// Environment variable name that holds the API key.
        /// </summary>
        public string ApiKeyEnvVarName { get; set; } = "LLM_API_KEY";
    }
}
