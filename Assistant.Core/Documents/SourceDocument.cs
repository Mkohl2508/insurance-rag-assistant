namespace Assistant.Core.Documents;

/// <summary>
/// A loaded source file together with its metadata and the pages that contain extractable text.
/// </summary>
/// <param name="FileName">File name without directory, e.g. "vgb-2022.pdf".</param>
/// <param name="FilePath">Absolute path of the file.</param>
/// <param name="Format">File format the document was loaded from.</param>
/// <param name="Pages">Pages with extractable text, ordered by page number.</param>
/// <param name="TotalPageCount">Number of pages in the file, including pages without text.</param>
/// <param name="SizeBytes">File size in bytes.</param>
/// <param name="LastModified">Last write time of the file.</param>
/// <param name="ContentHash">Lowercase hex SHA-256 hash of the file content.</param>
/// <param name="Title">Title from the document metadata, or null if not set.</param>
/// <param name="Author">Author from the document metadata, or null if not set.</param>
/// <param name="CreatedAt">Creation date from the document metadata, or null if not set or unparsable.</param>
public sealed record SourceDocument(
    string FileName,
    string FilePath,
    DocumentFormat Format,
    IReadOnlyList<DocumentPage> Pages,
    int TotalPageCount,
    long SizeBytes,
    DateTimeOffset LastModified,
    string ContentHash,
    string? Title,
    string? Author,
    DateTimeOffset? CreatedAt);
