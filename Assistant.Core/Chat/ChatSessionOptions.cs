namespace Assistant.Core.Chat;

/// <summary>
/// Provider-neutrale Einstellungen für eine <see cref="ChatSession"/>.
/// </summary>
public sealed class ChatSessionOptions
{
    public const string SectionName = "OpenAI:Chat";

    public string? SystemPrompt { get; set; }
    public float? Temperature { get; set; }
    public int? MaxOutputTokens { get; set; }
}
