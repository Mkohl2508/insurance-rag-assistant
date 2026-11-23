using Assistant.Core.Documents;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Assistant.Core.Tests.Documents;

public sealed class PdfDocumentLoaderTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("pdf-loader-tests").FullName;
    private readonly PdfDocumentLoader _loader = new();

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public void LoadFile_ReturnsOnePagePerPdfPage()
    {
        var path = CreatePdf("policy.pdf", ["Fire damage", "Water damage"]);

        var document = _loader.LoadFile(path);

        Assert.Equal([new DocumentPage(1, "Fire damage"), new DocumentPage(2, "Water damage")], document.Pages);
    }

    [Fact]
    public void LoadFile_ReturnsFileMetadata()
    {
        var path = CreatePdf("policy.pdf", ["Fire damage"]);

        var document = _loader.LoadFile(path);

        Assert.Equal("policy.pdf", document.FileName);
        Assert.Equal(Path.GetFullPath(path), document.FilePath);
        Assert.Equal(DocumentFormat.Pdf, document.Format);
        Assert.Equal(new FileInfo(path).Length, document.SizeBytes);
        Assert.Equal(File.GetLastWriteTimeUtc(path), document.LastModified.UtcDateTime);
        Assert.Matches("^[0-9a-f]{64}$", document.ContentHash);
    }

    [Fact]
    public void LoadFile_SkipsPagesWithoutTextButCountsThem()
    {
        var path = CreatePdf("policy.pdf", ["Cover", "", "Exclusions"]);

        var document = _loader.LoadFile(path);

        Assert.Equal([1, 3], document.Pages.Select(p => p.PageNumber));
        Assert.Equal(3, document.TotalPageCount);
    }

    [Fact]
    public void LoadFile_ReadsTitleAndAuthorFromPdfInformation()
    {
        var path = CreatePdf("policy.pdf", ["Text"], title: "Home Insurance Terms", author: "ACME Insurance");

        var document = _loader.LoadFile(path);

        Assert.Equal("Home Insurance Terms", document.Title);
        Assert.Equal("ACME Insurance", document.Author);
    }

    [Fact]
    public void LoadFile_ReturnsNullForBlankTitleAndAuthor()
    {
        var path = CreatePdf("policy.pdf", ["Text"], title: "  ");

        var document = _loader.LoadFile(path);

        Assert.Null(document.Title);
        Assert.Null(document.Author);
    }

    [Fact]
    public void LoadFile_ContentHashDependsOnlyOnContent()
    {
        var first = _loader.LoadFile(CreatePdf("a.pdf", ["Same"]));
        var copy = Path.Combine(_directory, "copy.pdf");
        File.Copy(first.FilePath, copy);
        var different = _loader.LoadFile(CreatePdf("c.pdf", ["Different"]));

        Assert.Equal(first.ContentHash, _loader.LoadFile(copy).ContentHash);
        Assert.NotEqual(first.ContentHash, different.ContentHash);
    }

    [Fact]
    public void LoadFile_InvalidPdf_ThrowsWithFileName()
    {
        var path = Path.Combine(_directory, "broken.pdf");
        File.WriteAllText(path, "not a pdf");

        var ex = Assert.Throws<InvalidDataException>(() => _loader.LoadFile(path));

        Assert.Contains("broken.pdf", ex.Message);
    }

    [Fact]
    public void LoadDirectory_LoadsOnePdfDocumentPerFileOrderedByName()
    {
        CreatePdf("b.pdf", ["Second"]);
        CreatePdf("a.pdf", ["First"]);
        File.WriteAllText(Path.Combine(_directory, "notes.txt"), "ignored");

        var documents = _loader.LoadDirectory(_directory);

        Assert.Equal(["a.pdf", "b.pdf"], documents.Select(d => d.FileName));
    }

    [Fact]
    public void LoadDirectory_SkipsFilesWithDuplicateContent()
    {
        var original = CreatePdf("a.pdf", ["Same"]);
        File.Copy(original, Path.Combine(_directory, "b.pdf"));

        var documents = _loader.LoadDirectory(_directory);

        Assert.Equal(["a.pdf"], documents.Select(d => d.FileName));
    }

    [Fact]
    public void LoadDirectory_MatchesExtensionCaseInsensitively()
    {
        CreatePdf("UPPER.PDF", ["Text"]);

        var documents = _loader.LoadDirectory(_directory);

        Assert.Equal(["UPPER.PDF"], documents.Select(d => d.FileName));
    }

    [Fact]
    public void LoadDirectory_ThrowsWhenDirectoryIsMissing()
    {
        var missing = Path.Combine(_directory, "missing");

        Assert.Throws<DirectoryNotFoundException>(() => _loader.LoadDirectory(missing));
    }

    private string CreatePdf(string fileName, string[] pageTexts, string? title = null, string? author = null)
    {
        var builder = new PdfDocumentBuilder();
        builder.DocumentInformation.Title = title;
        builder.DocumentInformation.Author = author;
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        foreach (var text in pageTexts)
        {
            var page = builder.AddPage(PageSize.A4);
            if (text.Length > 0)
                page.AddText(text, 12, new PdfPoint(50, 700), font);
        }

        var path = Path.Combine(_directory, fileName);
        File.WriteAllBytes(path, builder.Build());
        return path;
    }
}
