using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using System.Security.Cryptography;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Assistant.Core.Documents;

/// <summary>
/// Reads PDF files page by page. Pages without extractable text (e.g. scanned images) are skipped.
/// </summary>
public sealed class PdfDocumentLoader
{
    private readonly ILogger _logger;

    public PdfDocumentLoader(ILogger<PdfDocumentLoader>? logger = null)
    {
        _logger = logger ?? NullLogger<PdfDocumentLoader>.Instance;
    }

    /// <summary>Loads a single PDF file with its metadata and all pages that contain text.</summary>
    /// <exception cref="InvalidDataException">The file is not a readable PDF; the message names the file.</exception>
    public SourceDocument LoadFile(string path)
    {
        var stopwatch = Stopwatch.StartNew();
        var document = LoadWithoutLogging(path);
        LogLoaded(document, stopwatch.ElapsedMilliseconds);
        return document;
    }

    /// <exception cref="InvalidDataException">The file is not a readable PDF; the message names the file.</exception>
    private SourceDocument LoadWithoutLogging(string path)
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

    private void LogLoaded(SourceDocument document, long elapsedMs)
    {
        _logger.LogInformation("Loaded '{FileName}': {PagesWithText}/{TotalPages} pages with text, {ElapsedMs} ms",
            document.FileName, document.Pages.Count, document.TotalPageCount, elapsedMs);
        if (document.Pages.Count == 0)
            _logger.LogWarning("'{FileName}' contains no extractable text (scanned PDF?) and will produce no chunks",
                document.FileName);
    }

    private SourceDocument Read(FileInfo file, byte[] bytes)
    {
        using var document = PdfDocument.Open(bytes);

        var pages = new List<DocumentPage>();
        foreach (var page in document.GetPages())
        {
            var text = ContentOrderTextExtractor.GetText(page).Trim();
            if (text.Length > 0)
                pages.Add(new DocumentPage(page.Number, text));
            else
                _logger.LogDebug("'{FileName}' page {PageNumber} has no text, skipped", file.Name, page.Number);
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

    /// <summary>
    /// Loads all *.pdf files in a directory (not recursive), ordered by file name.
    /// Files with identical content are loaded once (the first by name), so chunk IDs stay unique.
    /// Files that cannot be read are logged as a warning and skipped.
    /// </summary>
    /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
    public IReadOnlyList<SourceDocument> LoadDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException($"Document directory '{Path.GetFullPath(directory)}' does not exist.");

        var files = Directory.EnumerateFiles(directory, "*.pdf", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive })
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
        _logger.LogInformation("Loading {FileCount} PDF files from '{Directory}'", files.Count, Path.GetFullPath(directory));

        var documents = new List<SourceDocument>();
        var byHash = new Dictionary<string, SourceDocument>();
        foreach (var path in files)
        {
            var stopwatch = Stopwatch.StartNew();
            SourceDocument document;
            try
            {
                document = LoadWithoutLogging(path);
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning("Skipped '{FileName}': {Error}", Path.GetFileName(path), ex.Message);
                continue;
            }

            if (byHash.TryAdd(document.ContentHash, document))
            {
                documents.Add(document);
                LogLoaded(document, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation("Skipped '{FileName}': same content as '{OriginalFileName}'",
                    document.FileName, byHash[document.ContentHash].FileName);
            }
        }

        return documents;
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
