using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Assistant.Core.Documents;

/// <summary>
/// Reads PDF files page by page. Pages without extractable text (e.g. scanned images) are skipped.
/// </summary>
public sealed class PdfDocumentLoader
{
    /// <summary>Loads all pages of a single PDF file.</summary>
    public IReadOnlyList<DocumentPage> LoadFile(string path)
    {
        var fileName = Path.GetFileName(path);
        using var document = PdfDocument.Open(path);

        var pages = new List<DocumentPage>();
        foreach (var page in document.GetPages())
        {
            var text = ContentOrderTextExtractor.GetText(page).Trim();
            if (text.Length > 0)
                pages.Add(new DocumentPage(fileName, page.Number, text));
        }

        return pages;
    }

    /// <summary>Loads all *.pdf files in a directory (not recursive), ordered by file name.</summary>
    /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
    public IReadOnlyList<DocumentPage> LoadDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException($"Document directory '{Path.GetFullPath(directory)}' does not exist.");

        return Directory.EnumerateFiles(directory, "*.pdf")
            .Order(StringComparer.OrdinalIgnoreCase)
            .SelectMany(LoadFile)
            .ToList();
    }
}
