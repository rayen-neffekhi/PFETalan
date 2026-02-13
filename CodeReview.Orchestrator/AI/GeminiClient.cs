using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CodeReview.Orchestrator.Configuration;
using CodeReview.Orchestrator.Logging;

namespace CodeReview.Orchestrator.AI
{
    /// <summary>
    /// Google Gemini API implementation of <see cref="ILLMClient"/>.
    /// Connects to Google Gemini API using endpoint and API key from environment variables.
    /// </summary>
    public class GeminiClient : ILLMClient
    {
        private readonly PipelineLogger _logger;
        private readonly HttpClient _httpClient;

        public GeminiClient(PipelineLogger logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Sends a prompt to Google Gemini API and returns the text completion.
        /// </summary>
        /// <param name="prompt">The text prompt to send to the model.</param>
        /// <param name="geminiSettings">Gemini API settings (endpoint, API key variable name).</param>
        /// <returns>Completion text from the model.</returns>
        public async Task<string> SendPromptAsync(string prompt, LLMSettings llmSettings)
        {
            if (llmSettings is not GeminiSettings geminiSettings)
            {
                throw new ArgumentException($"Expected GeminiSettings but got {llmSettings?.GetType().Name}", nameof(llmSettings));
            }

            try
            {
                _logger.LogInformation("GeminiClient: Preparing to send prompt to Google Gemini API...");

                // Read API key from environment variable
                string? apiKey = Environment.GetEnvironmentVariable(geminiSettings.ApiKeyEnvVarName);
                if (string.IsNullOrWhiteSpace(apiKey))
                    throw new Exception($"Gemini API key not found in environment variable '{geminiSettings.ApiKeyEnvVarName}'");

                // Build the Gemini API endpoint with API key query parameter
                string endpoint = $"{geminiSettings.Endpoint.TrimEnd('/')}?key={Uri.EscapeDataString(apiKey)}";

                // Create the request body in Gemini format
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    }
                };

                // Set headers (Gemini expects JSON content type)
                _httpClient.DefaultRequestHeaders.Clear();
                

                // Send the request
                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Gemini API error: {response.StatusCode} - {responseContent}");
                }

                // Parse the response
                using (JsonDocument doc = JsonDocument.Parse(responseContent))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("candidates", out JsonElement candidates) &&
                        candidates.GetArrayLength() > 0)
                    {
                        var firstCandidate = candidates[0];
                        if (firstCandidate.TryGetProperty("content", out JsonElement contentObj) &&
                            contentObj.TryGetProperty("parts", out JsonElement parts) &&
                            parts.GetArrayLength() > 0)
                        {
                            var firstPart = parts[0];
                            if (firstPart.TryGetProperty("text", out JsonElement textElement))
                            {
                                string result = textElement.GetString()?.Trim() ?? string.Empty;
                                _logger.LogInformation("GeminiClient: Received completion from Gemini API.");
                                return result;
                            }
                        }
                    }
                }

                throw new Exception("Failed to extract text response from Gemini API");
            }
            catch (Exception ex)
            {
                _logger.LogError($"GeminiClient: Error sending prompt - {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }
    }
}
