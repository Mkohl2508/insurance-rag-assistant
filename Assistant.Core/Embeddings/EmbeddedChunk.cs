using Assistant.Core.Documents;

namespace Assistant.Core.Embeddings;

/// <summary>
/// A chunk together with the embedding vector generated for its text.
/// </summary>
public sealed record EmbeddedChunk(DocumentChunk Chunk, ReadOnlyMemory<float> Vector);
