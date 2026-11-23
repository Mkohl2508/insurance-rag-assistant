using Assistant.Core.Documents;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace Assistant.Core.Embeddings;

/// <summary>
/// Sends chunk texts to an embedding model in batches and pairs each returned vector with its chunk.
/// Chunks with blank text are skipped. If a batch fails, the whole call fails.
/// </summary>
public sealed class ChunkEmbedder
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;
    private readonly int _batchSize;
    private readonly ILogger _logger;

    /// <param name="batchSize">Maximum number of chunks per request.</param>
    public ChunkEmbedder(
        IEmbeddingGenerator<string, Embedding<float>> generator, int batchSize = 100, ILogger<ChunkEmbedder>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        _generator = generator;
        _batchSize = batchSize;
        _logger = logger ?? NullLogger<ChunkEmbedder>.Instance;
    }

    /// <exception cref="InvalidOperationException">A request failed or returned an unusable result.</exception>
    public async Task<IReadOnlyList<EmbeddedChunk>> EmbedAsync(
        IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        var batches = chunks.Where(c => !string.IsNullOrWhiteSpace(c.Text)).Chunk(_batchSize).ToList();
        _logger.LogInformation("Embedding {ChunkCount} chunks in {BatchCount} batches",
            batches.Sum(b => b.Length), batches.Count);

        var result = new List<EmbeddedChunk>();
        for (var batchIndex = 0; batchIndex < batches.Count; batchIndex++)
        {
            var batch = batches[batchIndex];
            var stopwatch = Stopwatch.StartNew();
            var embeddings = await GenerateAsync(batch, cancellationToken);
            _logger.LogInformation("Batch {BatchNumber}/{BatchCount}: {ChunkCount} chunks ({Chunks}), {ElapsedMs} ms",
                batchIndex + 1, batches.Count, batch.Length, Describe(batch), stopwatch.ElapsedMilliseconds);

            for (var i = 0; i < batch.Length; i++)
                result.Add(new EmbeddedChunk(batch[i], embeddings[i].Vector));
        }

        return result;
    }

    private async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        DocumentChunk[] batch, CancellationToken cancellationToken)
    {
        GeneratedEmbeddings<Embedding<float>> embeddings;
        try
        {
            embeddings = await _generator.GenerateAsync(batch.Select(c => c.Text), cancellationToken: cancellationToken);
        }
        // Only cancellation requested by the caller passes through; timeouts also surface as
        // OperationCanceledException and are wrapped like any other failure.
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException($"Embedding request failed for chunks {Describe(batch)}: {ex.Message}", ex);
        }

        if (embeddings.Count != batch.Length)
            throw new InvalidOperationException(
                $"Expected {batch.Length} embeddings but received {embeddings.Count} for chunks {Describe(batch)}.");

        if (embeddings.Any(e => e.Vector.IsEmpty))
            throw new InvalidOperationException($"Received an empty embedding vector for chunks {Describe(batch)}.");

        return embeddings;
    }

    private static string Describe(DocumentChunk[] batch) =>
        batch.Length == 1 ? $"'{batch[0].Id}'" : $"'{batch[0].Id}' to '{batch[^1].Id}'";
}
