using Microsoft.Extensions.AI;

namespace Assistant.Core.Chat;

/// <summary>
/// Token usage of a single request or accumulated over a session. Basis for later cost reporting.
/// </summary>
public sealed record TokenUsage(long InputTokens, long OutputTokens, long TotalTokens)
{
    public static TokenUsage Empty { get; } = new(0, 0, 0);

    public static TokenUsage From(UsageDetails? usage)
    {
        if (usage is null) return Empty;

        var input = usage.InputTokenCount ?? 0;
        var output = usage.OutputTokenCount ?? 0;
        return new TokenUsage(input, output, usage.TotalTokenCount ?? input + output);
    }

    public static TokenUsage operator +(TokenUsage a, TokenUsage b) =>
        new(a.InputTokens + b.InputTokens, a.OutputTokens + b.OutputTokens, a.TotalTokens + b.TotalTokens);
}
