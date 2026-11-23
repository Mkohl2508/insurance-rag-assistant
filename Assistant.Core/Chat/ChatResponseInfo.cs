namespace Assistant.Core.Chat;

/// <summary>
/// Result of a single request to the LLM, including metadata.
/// </summary>
public sealed record ChatResponseInfo(
    string Text,
    string? Model,
    TokenUsage Usage,
    string? FinishReason,
    TimeSpan Duration);
