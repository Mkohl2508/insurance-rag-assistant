using Assistant.Core.Documents;

namespace Assistant.Core.Tests.Documents;

public class DocumentChunkerTests
{
    private const string Hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Fact]
    public void Chunk_ShortPage_ReturnsSingleChunk()
    {
        var chunks = new DocumentChunker(50, 10).Chunk(Doc(new DocumentPage(1, "Fire damage is covered.")));

        var chunk = Assert.Single(chunks);
        Assert.Equal("Fire damage is covered.", chunk.Text);
        Assert.Equal(1, chunk.PageNumber);
        Assert.Equal(0, chunk.ChunkIndex);
        Assert.Equal(Hash, chunk.DocumentId);
        Assert.Equal("policy.pdf", chunk.FileName);
    }

    [Fact]
    public void Chunk_LongPage_NoChunkExceedsChunkSize()
    {
        var chunks = new DocumentChunker(600, 100).Chunk(Doc(new DocumentPage(1, Words(400))));

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, c => Assert.InRange(c.Text.Length, 1, 600));
    }

    [Fact]
    public void Chunk_LongPage_CoversAllWords()
    {
        var text = Words(200);

        var chunks = new DocumentChunker(60, 15).Chunk(Doc(new DocumentPage(1, text)));

        var covered = chunks.SelectMany(c => c.Text.Split(' ')).ToHashSet();
        Assert.All(text.Split(' '), word => Assert.Contains(word, covered));
    }

    [Fact]
    public void Chunk_ConsecutiveChunksOverlap()
    {
        var chunks = new DocumentChunker(60, 15).Chunk(Doc(new DocumentPage(1, Words(50))));

        Assert.True(chunks.Count > 1);
        for (var i = 1; i < chunks.Count; i++)
        {
            // Words are unique, so the first word of a chunk must also appear in its predecessor.
            var firstWord = chunks[i].Text.Split(' ')[0];
            Assert.Contains(firstWord, chunks[i - 1].Text.Split(' '));
        }
    }

    [Fact]
    public void Chunk_PrefersSentenceEnd()
    {
        var text = "The insurer pays for fire damage. Storm damage is covered as well when wind exceeds force eight.";

        var chunks = new DocumentChunker(50, 10).Chunk(Doc(new DocumentPage(1, text)));

        Assert.Equal("The insurer pays for fire damage.", chunks[0].Text);
    }

    [Fact]
    public void Chunk_WithoutSentenceEnd_CutsAtWordBoundary()
    {
        var chunks = new DocumentChunker(20, 5).Chunk(Doc(new DocumentPage(1, "alpha beta gamma delta epsilon zeta")));

        Assert.Equal("alpha beta gamma", chunks[0].Text);
    }

    [Fact]
    public void Chunk_WithoutWhitespace_FallsBackToHardCut()
    {
        var chunks = new DocumentChunker(10, 3).Chunk(Doc(new DocumentPage(1, new string('x', 25))));

        Assert.Equal([10, 10, 5], chunks.Select(c => c.Text.Length));
    }

    [Fact]
    public void Chunk_DoesNotOverlapAcrossPages()
    {
        var chunks = new DocumentChunker(60, 15).Chunk(Doc(
            new DocumentPage(1, Words(30, prefix: "one")),
            new DocumentPage(2, Words(30, prefix: "two"))));

        var firstOfPageTwo = chunks.First(c => c.PageNumber == 2);
        Assert.Equal(0, firstOfPageTwo.ChunkIndex);
        Assert.StartsWith("two0 ", firstOfPageTwo.Text);
        Assert.DoesNotContain("one", firstOfPageTwo.Text);
        Assert.All(chunks.Where(c => c.PageNumber == 1), c => Assert.DoesNotContain("two", c.Text));
    }

    [Fact]
    public void Chunk_NormalizesWhitespace()
    {
        var chunks = new DocumentChunker().Chunk(Doc(new DocumentPage(1, "Fire\n\n  damage\t covered")));

        Assert.Equal("Fire damage covered", Assert.Single(chunks).Text);
    }

    [Fact]
    public void Chunk_SkipsBlankPages()
    {
        var chunks = new DocumentChunker().Chunk(Doc(new DocumentPage(1, " \n "), new DocumentPage(2, "Text")));

        Assert.Equal(2, Assert.Single(chunks).PageNumber);
    }

    [Fact]
    public void Chunk_IdsAreStableAndUnique()
    {
        var chunker = new DocumentChunker(60, 15);
        var document = Doc(new DocumentPage(1, Words(50)), new DocumentPage(3, "Short page."));

        var first = chunker.Chunk(document).Select(c => c.Id).ToList();
        var second = chunker.Chunk(document).Select(c => c.Id).ToList();

        Assert.Equal("0123456789ab-p1-c0", first[0]);
        Assert.Equal("0123456789ab-p3-c0", first[^1]);
        Assert.Equal(first.Count, first.Distinct().Count());
        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, -1)]
    [InlineData(100, 100)]
    public void Constructor_InvalidOptions_Throws(int chunkSize, int overlap)
    {
        Assert.ThrowsAny<ArgumentException>(() => new DocumentChunker(chunkSize, overlap));
    }

    private static string Words(int count, string prefix = "word") =>
        string.Join(' ', Enumerable.Range(0, count).Select(i => $"{prefix}{i}"));

    private static SourceDocument Doc(params DocumentPage[] pages) => new(
        FileName: "policy.pdf",
        FilePath: "/data/policy.pdf",
        Format: DocumentFormat.Pdf,
        Pages: pages,
        TotalPageCount: pages.Length,
        SizeBytes: 0,
        LastModified: DateTimeOffset.UnixEpoch,
        ContentHash: Hash,
        Title: null,
        Author: null,
        CreatedAt: null);
}
