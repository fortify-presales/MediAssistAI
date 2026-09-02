using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;

namespace MediAssistAI.Agents;

public static class SemanticKernelRegistration
{
    public static IServiceCollection AddMediAssistKernel(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var options = configuration.GetSection(OpenAiOptions.SectionName).Get<OpenAiOptions>() ?? new OpenAiOptions();
        Validate(options, environment);
        services.AddSingleton(options);

        if (!string.IsNullOrWhiteSpace(options.ApiKey) && !string.IsNullOrWhiteSpace(options.ModelId))
        {
            var kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.AddOpenAIChatCompletion(options.ModelId, options.ApiKey);
            services.AddSingleton(kernelBuilder.Build());
        }

        return services;
    }

    private static void Validate(OpenAiOptions options, IHostEnvironment environment)
    {
        if (options.TimeoutSeconds is < 1 or > 120 || options.MaxTokens is < 1 or > 2_000)
        {
            throw new InvalidOperationException("OpenAI limits must be within the configured safe bounds.");
        }

        if (!environment.IsDevelopment() && (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.ModelId)))
        {
            throw new InvalidOperationException("OpenAI:ApiKey and OpenAI:ModelId are required outside Development.");
        }
    }
}