using System.IO;
using System.Linq;
using Docx.TestSupport;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HtmlToDocx.Converter;

namespace HtmlToDocx.Converter.Tests;

public class DocxConverterTests
{
    private const int Success = 1;
    private const int Failure = -1;

    /// <summary>HTML that HtmlToOpenXml cannot parse: a data URI with an empty payload.</summary>
    private const string UnconvertibleHtml = "<p>before</p><img src=\"data:image/png;base64,\" /><p>after</p>";

    private static string BodyTextOf(string path)
    {
        using var document = WordprocessingDocument.Open(path, false);
        return document.MainDocumentPart!.Document.Body!.InnerText;
    }

    [Fact]
    public void ConvertToDocx_SimpleHtml_ReportsSuccess()
    {
        using var dir = new TempDirectory();
        var path = dir.NewFile(".docx");

        Assert.Equal(Success, new DocxConverter().ConvertToDocx("<p>Converted paragraph</p>", path));
    }

    [Fact]
    public void ConvertToDocx_SimpleHtml_WritesANonEmptyFile()
    {
        using var dir = new TempDirectory();
        var path = dir.NewFile(".docx");

        new DocxConverter().ConvertToDocx("<p>Converted paragraph</p>", path);

        Assert.True(File.Exists(path), "the output file must be created");
        Assert.True(new FileInfo(path).Length > 0, "the output file must not be empty");
    }

    [Fact]
    public void ConvertToDocx_SimpleHtml_WritesADocumentThatCanBeReopened()
    {
        using var dir = new TempDirectory();
        var path = dir.NewFile(".docx");

        new DocxConverter().ConvertToDocx("<p>Converted paragraph</p>", path);

        // Reporting success is worth nothing if the bytes on disk are not a package.
        using var document = WordprocessingDocument.Open(path, false);
        Assert.NotNull(document.MainDocumentPart);
        Assert.NotNull(document.MainDocumentPart!.Document.Body);
    }

    [Fact]
    public void ConvertToDocx_SimpleHtml_PreservesTheParagraphText()
    {
        using var dir = new TempDirectory();
        var path = dir.NewFile(".docx");

        new DocxConverter().ConvertToDocx("<p>Converted paragraph</p>", path);

        Assert.Contains("Converted paragraph", BodyTextOf(path));
    }

    [Fact]
    public void ConvertToDocx_SeveralParagraphs_PreservesAllOfThemInOrder()
    {
        using var dir = new TempDirectory();
        var path = dir.NewFile(".docx");

        new DocxConverter().ConvertToDocx("<p>First</p><p>Second</p><p>Third</p>", path);

        var text = BodyTextOf(path);
        Assert.True(text.IndexOf("First", System.StringComparison.Ordinal) >= 0
            && text.IndexOf("First", System.StringComparison.Ordinal) < text.IndexOf("Second", System.StringComparison.Ordinal)
            && text.IndexOf("Second", System.StringComparison.Ordinal) < text.IndexOf("Third", System.StringComparison.Ordinal),
            "paragraphs must keep their order, got: " + text);
    }

    [Fact]
    public void ConvertToDocx_BoldMarkup_ProducesABoldRun()
    {
        using var dir = new TempDirectory();
        var path = dir.NewFile(".docx");

        new DocxConverter().ConvertToDocx("<p>plain <b>bold</b></p>", path);

        using var document = WordprocessingDocument.Open(path, false);
        Assert.NotEmpty(document.MainDocumentPart!.Document.Body!.Descendants<Bold>());
    }

    [Fact]
    public void ConvertToDocx_TableMarkup_ProducesATable()
    {
        using var dir = new TempDirectory();
        var path = dir.NewFile(".docx");

        new DocxConverter().ConvertToDocx("<table><tr><td>cell one</td><td>cell two</td></tr></table>", path);

        using var document = WordprocessingDocument.Open(path, false);
        var body = document.MainDocumentPart!.Document.Body!;
        Assert.NotEmpty(body.Descendants<Table>());
        Assert.Contains("cell one", body.InnerText);
        Assert.Contains("cell two", body.InnerText);
    }

    [Fact]
    public void ConvertToDocx_EmptyHtml_WritesAValidEmptyDocument()
    {
        using var dir = new TempDirectory();
        var path = dir.NewFile(".docx");

        Assert.Equal(Success, new DocxConverter().ConvertToDocx(string.Empty, path));

        using var document = WordprocessingDocument.Open(path, false);
        Assert.NotNull(document.MainDocumentPart!.Document.Body);
    }

    [Fact]
    public void ConvertToDocx_NullHtml_WritesAValidEmptyDocument()
    {
        using var dir = new TempDirectory();
        var path = dir.NewFile(".docx");

        // HtmlToOpenXml treats a null document as an empty one; whatever the code says,
        // the file left behind has to be a package that opens.
        Assert.Equal(Success, new DocxConverter().ConvertToDocx(null!, path));

        using var document = WordprocessingDocument.Open(path, false);
        Assert.NotNull(document.MainDocumentPart!.Document.Body);
    }

    [Fact]
    public void ConvertToDocx_ExistingFile_IsReplacedByTheNewDocument()
    {
        using var dir = new TempDirectory();
        var path = dir.NewFile(".docx");
        new DocxConverter().ConvertToDocx("<p>Old content</p>", path);

        Assert.Equal(Success, new DocxConverter().ConvertToDocx("<p>New content</p>", path));

        var text = BodyTextOf(path);
        Assert.Contains("New content", text);
        Assert.DoesNotContain("Old content", text);
    }

    [Fact]
    public void ConvertToDocx_UnconvertibleHtml_ReportsFailure()
    {
        using var dir = new TempDirectory();
        var path = dir.NewFile(".docx");

        Assert.Equal(Failure, new DocxConverter().ConvertToDocx(UnconvertibleHtml, path));
    }

    [Fact]
    public void ConvertToDocx_UnconvertibleHtml_LeavesAnExistingFileIntact()
    {
        using var dir = new TempDirectory();
        var path = dir.NewFile(".docx");
        new DocxConverter().ConvertToDocx("<p>Document worth keeping</p>", path);
        var before = File.ReadAllBytes(path);

        var result = new DocxConverter().ConvertToDocx(UnconvertibleHtml, path);

        // A conversion that fails must not destroy the file it was going to replace.
        Assert.Equal(Failure, result);
        Assert.True(File.Exists(path), "the previous document must survive a failed conversion");
        Assert.Equal(before, File.ReadAllBytes(path));
    }

    [Fact]
    public void ConvertToDocx_PathInAMissingDirectory_ReportsFailure()
    {
        using var dir = new TempDirectory();

        Assert.Equal(Failure, new DocxConverter().ConvertToDocx("<p>x</p>", Path.Combine(dir.Path, "missing", "out.docx")));
    }

    [Fact]
    public void ConvertToDocx_DoesNotLeaveTheOutputFileLocked()
    {
        using var dir = new TempDirectory();
        var path = dir.NewFile(".docx");

        new DocxConverter().ConvertToDocx("<p>Unlocked afterwards</p>", path);

        using var reopened = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.True(reopened.Length > 0);
    }

    [Fact]
    public void ConvertToDocx_CalledTwice_ProducesEquivalentDocuments()
    {
        using var dir = new TempDirectory();
        var first = dir.NewFile(".docx");
        var second = dir.NewFile(".docx");

        new DocxConverter().ConvertToDocx("<p>Repeatable</p>", first);
        new DocxConverter().ConvertToDocx("<p>Repeatable</p>", second);

        Assert.Equal(BodyTextOf(first), BodyTextOf(second));
    }
}
