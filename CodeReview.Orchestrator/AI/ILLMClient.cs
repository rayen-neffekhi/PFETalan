using CodeReview.Orchestrator.Configuration;

namespace CodeReview.Orchestrator.AI
{
    /// <summary>
    /// Abstraction for LLM clients.
    /// </summary>
    public interface ILLMClient
    {
        /// <summary>
        /// Send a prompt to the LLM and return the textual response.
        /// Implementations should handle authentication and retries.
        /// </summary>
        Task<string> SendPromptAsync(string prompt, LLMSettings llmSettings);
    }
}
