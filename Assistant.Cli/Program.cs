using Assistant.Cli;
using Assistant.Core.Chat;
using System.Text;

// LLM answers can contain non-ASCII characters (quotes, dashes, currency signs).
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

AppConfiguration config;
ChatSession session;

try
{
    config = AppConfiguration.Load();
    session = new ChatSession(OpenAIChatClientFactory.Create(config.OpenAI), config.Chat);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Configuration error: {ex.Message}");
    return 1;
}

Console.WriteLine($"Chatting with {config.OpenAI.ChatModel}. '/reset' clears the history, '/exit' quits.");

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();

    if (input is null) break;

    var command = input.Trim();
    if (command.Length == 0) continue;

    if (command.StartsWith('/'))
    {
        if (command.Equals("/exit", StringComparison.OrdinalIgnoreCase)) break;

        if (command.Equals("/reset", StringComparison.OrdinalIgnoreCase))
        {
            session.Reset();
            Console.WriteLine("History cleared.");
        }
        else
        {
            Console.WriteLine($"Unknown command '{command}'. Available: /reset, /exit");
        }
        continue;
    }

    try
    {
        var result = await session.SendAsync(input);
        Console.WriteLine(result.Text);
        if (!result.IsComplete)
            Console.WriteLine($"[Warning: answer may be incomplete (finish reason: {result.FinishReason})]");
        Console.WriteLine(
            $"[{result.Model} | in {result.Usage.InputTokens} / out {result.Usage.OutputTokens} " +
            $"| {result.Duration.TotalSeconds:F1}s | since reset {session.TotalUsage.TotalTokens} " +
            $"| lifetime {session.LifetimeUsage.TotalTokens} tokens]");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

return 0;
