namespace Assistant.Core.Configuration;

/// <summary>
/// Connection settings for OpenAI. Bound to the "OpenAI" section.
/// Session behaviour (system prompt etc.) lives in <see cref="Chat.ChatSessionOptions"/> under "OpenAI:Chat".
/// </summary>
public sealed class OpenAIOptions
{
    public const string SectionName = "OpenAI";

    public string ApiKey { get; set; } = string.Empty;
    public string ChatModel { get; set; } = string.Empty;
    public string? EmbeddingModel { get; set; }
}
