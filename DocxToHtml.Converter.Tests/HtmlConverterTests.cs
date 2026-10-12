using System.IO;
using Docx.TestSupport;
using DocxToHtml.Converter;

namespace DocxToHtml.Converter.Tests;

public class HtmlConverterTests
{
    private const string UnsupportedFormat = "Unsupported format";
    private const string CorruptData = "File contains corrupt data";

    [Fact]
    public void ConvertToHtml_NullPath_ReturnsUnsupportedFormat()
    {
        Assert.Equal(UnsupportedFormat, new HtmlConverter().ConvertToHtml(null!));
    }

    [Fact]
    public void ConvertToHtml_EmptyPath_ReturnsUnsupportedFormat()
    {
        Assert.Equal(UnsupportedFormat, new HtmlConverter().ConvertToHtml(string.Empty));
    }

    [Theory]
    [InlineData("document.txt")]
    [InlineData("document.doc")]
    [InlineData("document.docm")]
    [InlineData("document")]
    [InlineData("document.docx.txt")]
    public void ConvertToHtml_PathWithoutDocxExtension_ReturnsUnsupportedFormat(string path)
    {
        Assert.Equal(UnsupportedFormat, new HtmlConverter().ConvertToHtml(path));
    }

    [Theory]
    [InlineData(".docx")]
    [InlineData(".DOCX")]
    [InlineData(".Docx")]
    [InlineData(".dOcX")]
    public void ConvertToHtml_DocxExtensionInAnyCase_ConvertsTheDocument(string extension)
    {
        // Extensions are case-insensitive on the file systems .docx lives on, so the
        // casing of the name must not decide whether a document is convertible.
        using var dir = new TempDirectory();
        var path = dir.NewFile(extension);
        DocxTestDocuments.Write(path, "Case insensitive extension");

        var html = new HtmlConverter().ConvertToHtml(path);

        Assert.Contains("Case insensitive extension", html);
    }

    [Fact]
    public void ConvertToHtml_SimpleDocument_ReturnsHtmlDocument()
    {
        using var dir = new TempDirectory();
        var path = dir.NewFile(".docx");
        DocxTestDocuments.Write(path, "Body text of the document");

        var html = new HtmlConverter().ConvertToHtml(path);

        Assert.StartsWith("<!DOCTYPE", html);
        Assert.Contains("<html", html);
        Assert.Contains("Body text of the document", html);
    }

    [Fact]
    public void ConvertToHtml_SimpleDocument_EmitsTheConfiguredAdditionalCss()
    {
        using var dir = new TempDirectory();
        var path = dir.NewFile(".docx");
        DocxTestDocuments.Write(path, "Styled document");

        var html = new HtmlConverter().ConvertToHtml(path);

        Assert.Contains("max-width: 20cm", html);
    }

    [Fact]
    public void ConvertToHtml_SimpleDocument_PrefixesFabricatedCssClasses()
    {
        using var dir = new TempDirectory();
        var path = dir.NewFile(".docx");
        DocxTestDocuments.Write(path, "Classified document");

        var html = new HtmlConverter().ConvertToHtml(path);

        Assert.Contains("pt-", html);
    }

    [Fact]
    public void ConvertToHtml_MultipleParagraphs_PreservesDocumentOrder()
    {
        using var dir = new TempDirectory();
        var path = dir.NewFile(".docx");
        DocxTestDocuments.Write(path, "First paragraph", "Second paragraph", "Third paragraph");

        var html = new HtmlConverter().ConvertToHtml(path);

        var first = html.IndexOf("First paragraph", System.StringComparison.Ordinal);
        var second = html.IndexOf("Second paragraph", System.StringComparison.Ordinal);
        var third = html.IndexOf("Third paragraph", System.StringComparison.Ordinal);
        Assert.True(first >= 0 && second >= 0 && third >= 0, "all paragraphs must be converted");
        Assert.True(first < second && second < third, "paragraphs must keep document order");
    }

    [Fact]
    public void ConvertToHtml_DocumentWithCorePropertiesTitle_UsesItAsPageTitle()
    {
        using var dir = new TempDirectory();
        var path = dir.NewFile(".docx");
        DocxTestDocuments.WriteWithTitle(path, "A Title From Core Properties", "Body");

        var html = new HtmlConverter().ConvertToHtml(path);

        Assert.Contains("<title>A Title From Core Properties</title>", html);
    }

    [Fact]
    public void ConvertToHtml_DocumentWithoutCorePropertiesTitle_UsesTheFilePathAsPageTitle()
    {
        using var dir = new TempDirectory();
        var path = dir.NewFile(".docx");
        DocxTestDocuments.WriteWithTitle(path, null, "Body");

        var html = new HtmlConverter().ConvertToHtml(path);

        Assert.Contains("<title>" + path + "</title>", html);
    }

    [Fact]
    public void ConvertToHtml_CorruptFile_ReturnsCorruptDataMessage()
    {
        using var dir = new TempDirectory();
        var path = dir.NewFile(".docx");
        DocxTestDocuments.WriteCorrupt(path);

        Assert.Equal(CorruptData, new HtmlConverter().ConvertToHtml(path));
    }

    [Fact]
    public void ConvertToHtml_MissingFile_ReportsFailureWithoutThrowing()
    {
        using var dir = new TempDirectory();

        // The contract of this method is to report failure in its return value; no
        // exception may escape for input it cannot read.
        var html = new HtmlConverter().ConvertToHtml(dir.File("does-not-exist.docx"));

        Assert.False(string.IsNullOrEmpty(html), "a failure must be reported, not returned as an empty string");
    }

    [Fact]
    public void ConvertToHtml_DocumentWithoutStylesPart_ConvertsTheDocument()
    {
        using var dir = new TempDirectory();
        var path = dir.NewFile(".docx");
        DocxTestDocuments.WriteWithoutStyles(path, "Document without a styles part");

        // Styles, numbering and settings are optional parts: a document carrying only a
        // main part is valid and must convert like any other.
        var html = new HtmlConverter().ConvertToHtml(path);

        Assert.Contains("Document without a styles part", html);
    }

    [Fact]
    public void ConvertToHtml_DoesNotModifyTheSourceDocument()
    {
        using var dir = new TempDirectory();
        var path = dir.NewFile(".docx");
        DocxTestDocuments.WriteWithoutStyles(path, "Read-only source");
        var before = File.ReadAllBytes(path);

        new HtmlConverter().ConvertToHtml(path);

        // Conversion is a read: whatever it has to add to make a document convertible
        // belongs to its in-memory copy.
        Assert.Equal(before, File.ReadAllBytes(path));
    }

    [Fact]
    public void ConvertToHtml_DocumentWithInvalidHyperlinkTarget_ConvertsTheDocument()
    {
        using var dir = new TempDirectory();
        var path = dir.NewFile(".docx");
        DocxTestDocuments.Write(path, "Document with a broken link");
        DocxTestDocuments.AddExternalRelationship(path, "mailto:first@example.com second@example.com");

        var html = new HtmlConverter().ConvertToHtml(path);

        Assert.Contains("Document with a broken link", html);
    }

    [Fact]
    public void ConvertToHtml_CalledTwiceOnTheSameFile_ReturnsTheSameHtml()
    {
        using var dir = new TempDirectory();
        var path = dir.NewFile(".docx");
        DocxTestDocuments.Write(path, "Repeatable conversion");

        var converter = new HtmlConverter();
        Assert.Equal(converter.ConvertToHtml(path), converter.ConvertToHtml(path));
    }

    [Fact]
    public void ConvertToHtml_DoesNotLeaveTheSourceFileLocked()
    {
        using var dir = new TempDirectory();
        var path = dir.NewFile(".docx");
        DocxTestDocuments.Write(path, "Unlocked afterwards");

        new HtmlConverter().ConvertToHtml(path);

        using var reopened = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.True(reopened.Length > 0);
    }
}
