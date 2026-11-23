using Assistant.Core.Chat;
using Assistant.Core.Configuration;
using Assistant.Core.Documents;
using Assistant.Core.Embeddings;
using System.Diagnostics;

namespace Assistant.Cli;

/// <summary>
/// Read-eval-print loop: reads user input, runs slash commands or sends the input to the chat session.
/// </summary>
internal sealed class ChatRepl
{
    private sealed record Command(string Description, Func<Task> Execute);

    private const int PreviewLength = 80;

    private readonly ChatSession _session;
    private readonly OpenAIOptions _openAI;
    private readonly DocumentOptions _documents;
    private readonly Dictionary<string, Command> _commands;
    private ChunkEmbedder? _embedder;
    private bool _exitRequested;

    public ChatRepl(ChatSession session, OpenAIOptions openAI, DocumentOptions documents)
    {
        _session = session;
        _openAI = openAI;
        _documents = documents;
        _commands = new Dictionary<string, Command>(StringComparer.OrdinalIgnoreCase)
        {
            ["/reset"] = new("clears the history", Sync(Reset)),
            ["/pages"] = new("lists the loaded PDF pages", Sync(ShowPages)),
            ["/chunks"] = new("lists the chunks of the loaded PDFs", Sync(ShowChunks)),
            ["/embed"] = new("embeds the chunks of the loaded PDFs", EmbedChunksAsync),
            ["/exit"] = new("quits", Sync(() => _exitRequested = true)),
        };
    }

    public async Task RunAsync()
    {
        var help = string.Join(", ", _commands.Select(c => $"'{c.Key}' {c.Value.Description}"));
        Console.WriteLine($"Chatting with {_openAI.ChatModel}. {help}.");

        while (!_exitRequested)
        {
            Console.Write("> ");
            var input = Console.ReadLine();

            if (input is null) return;

            var trimmed = input.Trim();
            if (trimmed.Length == 0) continue;

            if (trimmed.StartsWith('/'))
                await RunCommandAsync(trimmed);
            else
                await SendAsync(input);
        }
    }

    private static Func<Task> Sync(Action action) => () =>
    {
        action();
        return Task.CompletedTask;
    };

    private async Task RunCommandAsync(string name)
    {
        if (_commands.TryGetValue(name, out var command))
            await command.Execute();
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
        foreach (var document in LoadDocuments())
        {
            var title = document.Title is null ? "" : $" \"{document.Title}\"";
            Console.WriteLine(
                $"{document.FileName}{title} ({document.Pages.Count}/{document.TotalPageCount} pages with text, " +
                $"{document.SizeBytes / 1024.0:F0} KB)");
            foreach (var page in document.Pages)
                Console.WriteLine($"  p{page.PageNumber,-3} {Preview(page.Text)}");
        }
    }

    private void ShowChunks()
    {
        var chunker = new DocumentChunker();
        foreach (var document in LoadDocuments())
        {
            var chunks = chunker.Chunk(document);
            Console.WriteLine($"{document.FileName} ({chunks.Count} chunks)");
            foreach (var chunk in chunks)
                Console.WriteLine($"  {chunk.Id}  {chunk.Text.Length,3}  {Preview(chunk.Text)}");
        }
    }

    private async Task EmbedChunksAsync()
    {
        var documents = LoadDocuments();
        if (documents.Count == 0) return;

        var chunker = new DocumentChunker();
        var chunks = documents.SelectMany(chunker.Chunk).ToList();
        if (chunks.Count == 0)
        {
            Console.WriteLine("No text chunks to embed.");
            return;
        }

        try
        {
            // Created on first use, so a missing embedding configuration does not prevent chatting.
            _embedder ??= new ChunkEmbedder(OpenAIEmbeddingGeneratorFactory.Create(_openAI));

            var stopwatch = Stopwatch.StartNew();
            var embedded = await _embedder.EmbedAsync(chunks);
            stopwatch.Stop();

            foreach (var document in embedded.GroupBy(e => e.Chunk.FileName))
                Console.WriteLine($"{document.Key}: {document.Count()} chunks embedded");

            var dimensions = embedded.FirstOrDefault()?.Vector.Length ?? 0;
            Console.WriteLine(
                $"{embedded.Count}/{chunks.Count} chunks embedded, {dimensions} dimensions, " +
                $"{stopwatch.Elapsed.TotalSeconds:F1} s");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    /// <summary>Loads all PDFs from the data directory; prints a message and returns none if loading fails or finds nothing.</summary>
    private IReadOnlyList<SourceDocument> LoadDocuments()
    {
        try
        {
            var documents = new PdfDocumentLoader().LoadDirectory(_documents.DataDirectory);
            if (documents.Count == 0)
                Console.WriteLine($"No PDF files found in '{Path.GetFullPath(_documents.DataDirectory)}'.");
            return documents;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return [];
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
