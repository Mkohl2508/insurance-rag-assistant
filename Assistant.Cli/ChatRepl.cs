using Assistant.Core.Chat;
using Assistant.Core.Documents;

namespace Assistant.Cli;

/// <summary>
/// Read-eval-print loop: reads user input, runs slash commands or sends the input to the chat session.
/// </summary>
internal sealed class ChatRepl
{
    private sealed record Command(string Description, Action Execute);

    private const int PreviewLength = 80;

    private readonly ChatSession _session;
    private readonly string _modelName;
    private readonly DocumentOptions _documents;
    private readonly Dictionary<string, Command> _commands;
    private bool _exitRequested;

    public ChatRepl(ChatSession session, string modelName, DocumentOptions documents)
    {
        _session = session;
        _modelName = modelName;
        _documents = documents;
        _commands = new Dictionary<string, Command>(StringComparer.OrdinalIgnoreCase)
        {
            ["/reset"] = new("clears the history", Reset),
            ["/pages"] = new("lists the loaded PDF pages", ShowPages),
            ["/exit"] = new("quits", () => _exitRequested = true),
        };
    }

    public async Task RunAsync()
    {
        var help = string.Join(", ", _commands.Select(c => $"'{c.Key}' {c.Value.Description}"));
        Console.WriteLine($"Chatting with {_modelName}. {help}.");

        while (!_exitRequested)
        {
            Console.Write("> ");
            var input = Console.ReadLine();

            if (input is null) return;

            var trimmed = input.Trim();
            if (trimmed.Length == 0) continue;

            if (trimmed.StartsWith('/'))
                RunCommand(trimmed);
            else
                await SendAsync(input);
        }
    }

    private void RunCommand(string name)
    {
        if (_commands.TryGetValue(name, out var command))
            command.Execute();
        else
            Console.WriteLine($"Unknown command '{name}'. Available: {string.Join(", ", _commands.Keys)}");
    }

    private void Reset()
    {
        _session.Reset();
        Console.WriteLine("History cleared.");
    }

    private void ShowPages()
    {
        try
        {
            var pages = new PdfDocumentLoader().LoadDirectory(_documents.DataDirectory);
            if (pages.Count == 0)
            {
                Console.WriteLine($"No PDF pages found in '{Path.GetFullPath(_documents.DataDirectory)}'.");
                return;
            }

            foreach (var file in pages.GroupBy(p => p.FileName))
            {
                Console.WriteLine($"{file.Key} ({file.Count()} pages)");
                foreach (var page in file)
                    Console.WriteLine($"  p{page.PageNumber,-3} {Preview(page.Text)}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static string Preview(string text)
    {
        var singleLine = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return singleLine.Length <= PreviewLength ? singleLine : singleLine[..PreviewLength] + "…";
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
