namespace Assistant.Core.Chat;

/// <summary>
/// Ergebnis einer einzelnen Anfrage an das LLM inkl. Metadaten.
/// </summary>
public sealed record ChatResponseInfo(
    string Text,
    string? Model,
    TokenUsage Usage,
    string? FinishReason,
    TimeSpan Duration);
