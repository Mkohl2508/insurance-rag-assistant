namespace Assistant.Core.Chat;

/// <summary>
/// Provider-neutral settings for a <see cref="ChatSession"/>.
/// </summary>
public sealed class ChatSessionOptions
{
    public const string SectionName = "OpenAI:Chat";

    public string? SystemPrompt { get; set; }
    public float? Temperature { get; set; }
    public int? MaxOutputTokens { get; set; }
}
