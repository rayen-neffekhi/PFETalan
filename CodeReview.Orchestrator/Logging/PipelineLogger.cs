using System;

namespace CodeReview.Orchestrator.Logging
{
    /// <summary>
    /// Simple console logger for pipeline traceability.
    /// Replace or extend with a structured logger as needed.
    /// </summary>
    public class PipelineLogger
    {
        public void LogInformation(string message)
        {
            Console.WriteLine($"[INFO] {DateTime.UtcNow:O} - {message}");
        }

        public void LogWarning(string message)
        {
            Console.WriteLine($"[WARN] {DateTime.UtcNow:O} - {message}");
        }

        public void LogError(string message)
        {
            Console.WriteLine($"[ERROR] {DateTime.UtcNow:O} - {message}");
        }
    }
}
