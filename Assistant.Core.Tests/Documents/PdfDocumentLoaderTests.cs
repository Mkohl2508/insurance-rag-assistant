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
    public void LoadFile_ReturnsOnePagePerPdfPageWithMetadata()
    {
        var path = CreatePdf("policy.pdf", "Fire damage", "Water damage");

        var pages = _loader.LoadFile(path);

        Assert.Equal(2, pages.Count);
        Assert.Equal(new DocumentPage("policy.pdf", 1, "Fire damage"), pages[0]);
        Assert.Equal(new DocumentPage("policy.pdf", 2, "Water damage"), pages[1]);
    }

    [Fact]
    public void LoadFile_SkipsPagesWithoutText()
    {
        var path = CreatePdf("policy.pdf", "Cover", "", "Exclusions");

        var pages = _loader.LoadFile(path);

        Assert.Equal([1, 3], pages.Select(p => p.PageNumber));
    }

    [Fact]
    public void LoadDirectory_LoadsOnlyPdfFilesOrderedByName()
    {
        CreatePdf("b.pdf", "Second");
        CreatePdf("a.pdf", "First");
        File.WriteAllText(Path.Combine(_directory, "notes.txt"), "ignored");

        var pages = _loader.LoadDirectory(_directory);

        Assert.Equal(["a.pdf", "b.pdf"], pages.Select(p => p.FileName));
    }

    [Fact]
    public void LoadDirectory_ThrowsWhenDirectoryIsMissing()
    {
        var missing = Path.Combine(_directory, "missing");

        Assert.Throws<DirectoryNotFoundException>(() => _loader.LoadDirectory(missing));
    }

    private string CreatePdf(string fileName, params string[] pageTexts)
    {
        var builder = new PdfDocumentBuilder();
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
