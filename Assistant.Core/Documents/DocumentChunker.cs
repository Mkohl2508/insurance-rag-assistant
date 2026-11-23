using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Assistant.Core.Documents;

/// <summary>
/// Splits each page of a document into chunks of at most chunkSize characters.
/// Consecutive chunks of a page overlap by about overlap characters; chunks never span pages.
/// Cuts prefer sentence ends, then word boundaries, and fall back to a hard cut.
/// </summary>
public sealed class DocumentChunker
{
    private const int IdHashLength = 12;

    private readonly int _chunkSize;
    private readonly int _overlap;
    private readonly ILogger _logger;

    /// <param name="chunkSize">Maximum chunk length in characters.</param>
    /// <param name="overlap">Approximate number of characters shared by consecutive chunks of a page.</param>
    public DocumentChunker(int chunkSize = 600, int overlap = 100, ILogger<DocumentChunker>? logger = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chunkSize);
        ArgumentOutOfRangeException.ThrowIfNegative(overlap);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(overlap, chunkSize);

        _chunkSize = chunkSize;
        _overlap = overlap;
        _logger = logger ?? NullLogger<DocumentChunker>.Instance;
    }

    public IReadOnlyList<DocumentChunk> Chunk(SourceDocument document)
    {
        var idPrefix = document.ContentHash[..IdHashLength];
        var chunks = new List<DocumentChunk>();

        foreach (var page in document.Pages)
        {
            var text = Normalize(page.Text);
            if (text.Length == 0)
                continue;

            var texts = SplitPage(text);
            for (var i = 0; i < texts.Count; i++)
            {
                chunks.Add(new DocumentChunk(
                    Id: $"{idPrefix}-p{page.PageNumber}-c{i}",
                    DocumentId: document.ContentHash,
                    FileName: document.FileName,
                    PageNumber: page.PageNumber,
                    ChunkIndex: i,
                    Text: texts[i]));
            }
        }

        _logger.LogDebug("Chunked '{FileName}': {ChunkCount} chunks from {PageCount} pages",
            document.FileName, chunks.Count, document.Pages.Count);
        return chunks;
    }

    private static string Normalize(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private List<string> SplitPage(string text)
    {
        var result = new List<string>();
        var start = 0;

        while (text.Length - start > _chunkSize)
        {
            var end = FindEnd(text, start);
            result.Add(text[start..end]);
            start = NextStart(text, end);
        }

        result.Add(text[start..]);
        return result;
    }

    /// <summary>
    /// Exclusive end of the chunk starting at <paramref name="start"/>. Requires text beyond the window.
    /// The lower bound keeps chunks from getting tiny and guarantees that the next start moves forward.
    /// </summary>
    private int FindEnd(string text, int start)
    {
        var limit = start + _chunkSize;
        var minEnd = start + Math.Max(_overlap + 1, _chunkSize / 2);

        for (var i = limit; i >= minEnd; i--)
        {
            if (text[i] == ' ' && text[i - 1] is '.' or '!' or '?')
                return i;
        }

        for (var i = limit; i >= minEnd; i--)
        {
            if (text[i] == ' ')
                return i;
        }

        return limit;
    }

    /// <summary>Start of the next chunk: about overlap characters before end, moved forward to a word start.</summary>
    private int NextStart(string text, int end)
    {
        var start = end - _overlap;
        while (start < end && text[start - 1] != ' ')
            start++;

        return text[start] == ' ' ? start + 1 : start;
    }
}
