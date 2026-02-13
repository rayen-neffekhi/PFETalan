using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CodeReview.Orchestrator.Configuration;
using CodeReview.Orchestrator.Pipeline;
using CodeReview.Orchestrator.AI;

namespace CodeReview.Orchestrator;

/// <summary>
/// Entry point for the CodeReview.Orchestrator console application.
/// </summary>
public static class Program
{
    /// <summary>
    /// Main program. Configures DI and runs the pipeline.
    /// </summary>
    public static async Task<int> Main(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, builder) =>
            {
                builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                services.Configure<AppSettings>(context.Configuration.GetSection("AppSettings"));
                services.Configure<GeminiSettings>(context.Configuration.GetSection("GeminiSettings"));
                services.AddSingleton<PipelineRunner>();
                services.AddSingleton<PipelineContext>();
                // Register implementations
                //services.AddSingleton<AI.ILLMClient, AI.MockLLMClient>();
                services.AddSingleton<ILLMClient, GeminiClient>();
                services.AddSingleton<Analysis.Sonar.SonarResultParser>();
                services.AddSingleton<Analysis.Roslyn.RoslynResultParser>();
                services.AddSingleton<Analysis.IssueAggregator>();
                services.AddSingleton<Prompting.PromptBuilder>();
                services.AddSingleton<Feedback.PullRequestCommenter>();
                services.AddSingleton<Feedback.ReviewSummaryBuilder>();
                services.AddSingleton<Logging.PipelineLogger>();
                services.AddSingleton<Utils.FileLoader>();
            })
            .Build();

        var logger = host.Services.GetRequiredService<Logging.PipelineLogger>();
        logger.LogInformation("Starting CodeReview.Orchestrator pipeline...");

        try
        {
            var runner = host.Services.GetRequiredService<PipelineRunner>();
            await runner.RunAsync();
            logger.LogInformation("Pipeline finished successfully.");
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError($"Pipeline failed: {ex.Message}");
            return 1;
        }
        finally
        {
            logger.LogInformation("Exiting.");
        }
    }
}
