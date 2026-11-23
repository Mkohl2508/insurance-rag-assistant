namespace Assistant.Core.Chat;

/// <summary>
/// Result of a single request to the LLM, including metadata.
/// </summary>
public sealed record ChatResponseInfo(
    string Text,
    string? Model,
    TokenUsage Usage,
    string? FinishReason,
    TimeSpan Duration)
{
    /// <summary>
    /// False if the model stopped for another reason than finishing its answer
    /// (e.g. token limit or content filter). An unknown finish reason counts as complete.
    /// </summary>
    public bool IsComplete => FinishReason is null || FinishReason == "stop";
}
