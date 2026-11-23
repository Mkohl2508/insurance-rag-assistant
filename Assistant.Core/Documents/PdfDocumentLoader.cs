using System.Security.Cryptography;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Assistant.Core.Documents;

/// <summary>
/// Reads PDF files page by page. Pages without extractable text (e.g. scanned images) are skipped.
/// </summary>
public sealed class PdfDocumentLoader
{
    /// <summary>Loads a single PDF file with its metadata and all pages that contain text.</summary>
    /// <exception cref="InvalidDataException">The file is not a readable PDF; the message names the file.</exception>
    public SourceDocument LoadFile(string path)
    {
        var file = new FileInfo(path);
        var bytes = File.ReadAllBytes(file.FullName);
        try
        {
            return Read(file, bytes);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Could not read PDF '{file.Name}': {ex.Message}", ex);
        }
    }

    private static SourceDocument Read(FileInfo file, byte[] bytes)
    {
        using var document = PdfDocument.Open(bytes);

        var pages = new List<DocumentPage>();
        foreach (var page in document.GetPages())
        {
            var text = ContentOrderTextExtractor.GetText(page).Trim();
            if (text.Length > 0)
                pages.Add(new DocumentPage(page.Number, text));
        }

        var info = document.Information;
        return new SourceDocument(
            FileName: file.Name,
            FilePath: file.FullName,
            Format: DocumentFormat.Pdf,
            Pages: pages,
            TotalPageCount: document.NumberOfPages,
            SizeBytes: file.Length,
            LastModified: file.LastWriteTimeUtc,
            ContentHash: Convert.ToHexStringLower(SHA256.HashData(bytes)),
            Title: NullIfBlank(info.Title),
            Author: NullIfBlank(info.Author),
            CreatedAt: info.GetCreatedDateTimeOffset());
    }

    /// <summary>Loads all *.pdf files in a directory (not recursive), ordered by file name.</summary>
    /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
    public IReadOnlyList<SourceDocument> LoadDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException($"Document directory '{Path.GetFullPath(directory)}' does not exist.");

        return Directory.EnumerateFiles(directory, "*.pdf")
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(LoadFile)
            .ToList();
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
