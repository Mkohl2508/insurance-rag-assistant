using Assistant.Core.Documents;
using Assistant.Core.Embeddings;
using Microsoft.Extensions.AI;

namespace Assistant.Core.Tests.Embeddings;

public class ChunkEmbedderTests
{
    private static DocumentChunk Chunk(int index, string text) =>
        new($"doc-p1-c{index}", "doc", "doc.pdf", 1, index, text);

    [Fact]
    public async Task EmbedAsync_SplitsIntoBatchesOfBatchSize()
    {
        var generator = new FakeEmbeddingGenerator();
        var chunks = Enumerable.Range(0, 5).Select(i => Chunk(i, $"text {i}")).ToList();

        await new ChunkEmbedder(generator, batchSize: 2).EmbedAsync(chunks);

        Assert.Equal([2, 2, 1], generator.ReceivedBatches.Select(b => b.Count));
    }

    [Fact]
    public async Task EmbedAsync_PairsEachVectorWithItsChunk()
    {
        var generator = new FakeEmbeddingGenerator();
        var chunks = new[] { Chunk(0, "a"), Chunk(1, "bbb"), Chunk(2, "cc") };

        var result = await new ChunkEmbedder(generator, batchSize: 2).EmbedAsync(chunks);

        Assert.Equal(chunks, result.Select(r => r.Chunk));
        Assert.Equal([1f, 3f, 2f], result.Select(r => r.Vector.Span[0]));
    }

    [Fact]
    public async Task EmbedAsync_SkipsBlankChunks()
    {
        var generator = new FakeEmbeddingGenerator();
        var chunks = new[] { Chunk(0, "a"), Chunk(1, ""), Chunk(2, "   "), Chunk(3, "b") };

        var result = await new ChunkEmbedder(generator).EmbedAsync(chunks);

        Assert.Equal(["a", "b"], Assert.Single(generator.ReceivedBatches));
        Assert.Equal(["doc-p1-c0", "doc-p1-c3"], result.Select(r => r.Chunk.Id));
    }

    [Fact]
    public async Task EmbedAsync_WithoutUsableChunks_SendsNothing()
    {
        var generator = new FakeEmbeddingGenerator();

        var result = await new ChunkEmbedder(generator).EmbedAsync([Chunk(0, " ")]);

        Assert.Empty(result);
        Assert.Empty(generator.ReceivedBatches);
    }

    [Fact]
    public async Task EmbedAsync_WrongNumberOfVectors_Throws()
    {
        var generator = new FakeEmbeddingGenerator
        {
            Handler = _ => new GeneratedEmbeddings<Embedding<float>>([new Embedding<float>(new float[] { 1 })]),
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new ChunkEmbedder(generator).EmbedAsync([Chunk(0, "a"), Chunk(1, "b")]));

        Assert.Contains("'doc-p1-c0' to 'doc-p1-c1'", ex.Message);
    }

    [Fact]
    public async Task EmbedAsync_EmptyVector_Throws()
    {
        var generator = new FakeEmbeddingGenerator
        {
            Handler = _ => new GeneratedEmbeddings<Embedding<float>>([new Embedding<float>(Array.Empty<float>())]),
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new ChunkEmbedder(generator).EmbedAsync([Chunk(0, "a")]));
    }

    [Fact]
    public async Task EmbedAsync_GeneratorFails_WrapsExceptionWithChunkIds()
    {
        var inner = new HttpRequestException("boom");
        var generator = new FakeEmbeddingGenerator { Handler = _ => throw inner };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new ChunkEmbedder(generator).EmbedAsync([Chunk(0, "a")]));

        Assert.Same(inner, ex.InnerException);
        Assert.Contains("'doc-p1-c0'", ex.Message);
    }

    [Fact]
    public async Task EmbedAsync_Cancelled_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        var generator = new FakeEmbeddingGenerator
        {
            Handler = _ =>
            {
                cts.Cancel();
                throw new OperationCanceledException(cts.Token);
            },
        };

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => new ChunkEmbedder(generator).EmbedAsync([Chunk(0, "a")], cts.Token));
    }

    [Fact]
    public async Task EmbedAsync_TimeoutWithoutCancellation_WrapsExceptionWithChunkIds()
    {
        var generator = new FakeEmbeddingGenerator { Handler = _ => throw new TaskCanceledException("timeout") };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new ChunkEmbedder(generator).EmbedAsync([Chunk(0, "a")]));

        Assert.IsType<TaskCanceledException>(ex.InnerException);
        Assert.Contains("'doc-p1-c0'", ex.Message);
    }

    [Fact]
    public void Constructor_RejectsNonPositiveBatchSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChunkEmbedder(new FakeEmbeddingGenerator(), batchSize: 0));
    }
}
