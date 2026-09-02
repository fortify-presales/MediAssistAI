namespace MediAssistAI.Agents;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    public string? ApiKey { get; init; }

    public string? ModelId { get; init; }

    public int TimeoutSeconds { get; init; } = 30;

    public int MaxTokens { get; init; } = 500;
}