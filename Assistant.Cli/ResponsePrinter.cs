using Assistant.Core.Chat;

namespace Assistant.Cli;

/// <summary>
/// Writes an LLM answer to the console, followed by a warning if needed and a status line.
/// </summary>
internal static class ResponsePrinter
{
    public static void Print(ChatResponseInfo result, TokenUsage sinceReset, TokenUsage lifetime)
    {
        Console.WriteLine(result.Text);

        if (!result.IsComplete)
            Console.WriteLine($"[Warning: answer may be incomplete (finish reason: {result.FinishReason})]");

        Console.WriteLine(
            $"[{result.Model} | in {result.Usage.InputTokens} / out {result.Usage.OutputTokens} " +
            $"| {result.Duration.TotalSeconds:F1}s | since reset {sinceReset.TotalTokens} " +
            $"| lifetime {lifetime.TotalTokens} tokens]");
    }
}
