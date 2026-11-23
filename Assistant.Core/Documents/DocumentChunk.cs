namespace Assistant.Core.Documents;

/// <summary>
/// A small, overlapping piece of a single page's text. Unit for embedding and retrieval.
/// </summary>
/// <param name="Id">Stable ID "{hash12}-p{page}-c{index}", e.g. "3fa9c2e1b07d-p4-c2".</param>
/// <param name="DocumentId">Content hash of the source document.</param>
/// <param name="FileName">File name of the source document, for citations.</param>
/// <param name="PageNumber">1-based page number the text comes from.</param>
/// <param name="ChunkIndex">0-based position of the chunk within its page.</param>
/// <param name="Text">Whitespace-normalized chunk text.</param>
public sealed record DocumentChunk(
    string Id,
    string DocumentId,
    string FileName,
    int PageNumber,
    int ChunkIndex,
    string Text);
