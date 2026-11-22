namespace Assistant.Core.Configuration;

/// <summary>
/// Verbindungseinstellungen für OpenAI. Wird an die Sektion "OpenAI" gebunden.
/// Session-Verhalten (SystemPrompt etc.) liegt in <see cref="Chat.ChatSessionOptions"/> unter "OpenAI:Chat".
/// </summary>
public sealed class OpenAIOptions
{
    public const string SectionName = "OpenAI";

    public string ApiKey { get; set; } = string.Empty;
    public string ChatModel { get; set; } = string.Empty;
    public string? EmbeddingModel { get; set; }
}
