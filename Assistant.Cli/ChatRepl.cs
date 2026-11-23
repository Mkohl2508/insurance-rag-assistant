using Assistant.Core.Chat;

namespace Assistant.Cli;

/// <summary>
/// Read-eval-print loop: reads user input, runs slash commands or sends the input to the chat session.
/// </summary>
internal sealed class ChatRepl
{
    /// <param name="Execute">Runs the command; returns false to end the loop.</param>
    private sealed record Command(string Description, Func<bool> Execute);

    private readonly ChatSession _session;
    private readonly string _modelName;
    private readonly Dictionary<string, Command> _commands;

    public ChatRepl(ChatSession session, string modelName)
    {
        ArgumentNullException.ThrowIfNull(session);

        _session = session;
        _modelName = modelName;
        _commands = new Dictionary<string, Command>(StringComparer.OrdinalIgnoreCase)
        {
            ["/reset"] = new("clears the history", Reset),
            ["/exit"] = new("quits", () => false),
        };
    }

    public async Task RunAsync()
    {
        var help = string.Join(", ", _commands.Select(c => $"'{c.Key}' {c.Value.Description}"));
        Console.WriteLine($"Chatting with {_modelName}. {help}.");

        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine();

            if (input is null) return;

            var trimmed = input.Trim();
            if (trimmed.Length == 0) continue;

            if (trimmed.StartsWith('/'))
            {
                if (!RunCommand(trimmed)) return;
                continue;
            }

            await SendAsync(input);
        }
    }

    /// <returns>False if the loop should end.</returns>
    private bool RunCommand(string name)
    {
        if (_commands.TryGetValue(name, out var command))
            return command.Execute();

        Console.WriteLine($"Unknown command '{name}'. Available: {string.Join(", ", _commands.Keys)}");
        return true;
    }

    private bool Reset()
    {
        _session.Reset();
        Console.WriteLine("History cleared.");
        return true;
    }

    private async Task SendAsync(string input)
    {
        try
        {
            var result = await _session.SendAsync(input);
            ResponsePrinter.Print(result, _session.TotalUsage, _session.LifetimeUsage);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
