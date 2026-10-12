using System;
using Docx.TestSupport;
using DocumentFormat.OpenXml.Packaging;

namespace DocxToHtml.Tests;

/// <summary>
/// The pipeline the application drives: a document converted to HTML and back, through
/// both converters in turn.
/// </summary>
public class ConversionRoundTripTests
{
    private static string BodyTextOf(string path)
    {
        using var document = WordprocessingDocument.Open(path, false);
        return document.MainDocumentPart!.Document.Body!.InnerText;
    }

    [Fact]
    public void DocxToHtmlToDocx_PreservesTheParagraphText()
    {
        using var dir = new TempDirectory();
        var source = dir.NewFile(".docx");
        var result = dir.NewFile(".docx");
        DocxTestDocuments.Write(source, "Text that survives a round trip");

        var html = new global::DocxToHtml.Converter.HtmlConverter().ConvertToHtml(source);
        var code = new global::HtmlToDocx.Converter.DocxConverter().ConvertToDocx(html, result);

        Assert.Equal(1, code);
        Assert.Contains("Text that survives a round trip", BodyTextOf(result));
    }

    [Fact]
    public void DocxToHtmlToDocx_PreservesEveryParagraph()
    {
        using var dir = new TempDirectory();
        var source = dir.NewFile(".docx");
        var result = dir.NewFile(".docx");
        DocxTestDocuments.Write(source, "Alpha paragraph", "Beta paragraph", "Gamma paragraph");

        var html = new global::DocxToHtml.Converter.HtmlConverter().ConvertToHtml(source);
        new global::HtmlToDocx.Converter.DocxConverter().ConvertToDocx(html, result);

        var text = BodyTextOf(result);
        Assert.Contains("Alpha paragraph", text);
        Assert.Contains("Beta paragraph", text);
        Assert.Contains("Gamma paragraph", text);
    }

    [Fact]
    public void DocxToHtmlToDocx_ProducesADocumentTheHtmlConverterCanReadAgain()
    {
        using var dir = new TempDirectory();
        var source = dir.NewFile(".docx");
        var result = dir.NewFile(".docx");
        DocxTestDocuments.Write(source, "Twice around the loop");

        var firstHtml = new global::DocxToHtml.Converter.HtmlConverter().ConvertToHtml(source);
        new global::HtmlToDocx.Converter.DocxConverter().ConvertToDocx(firstHtml, result);
        var secondHtml = new global::DocxToHtml.Converter.HtmlConverter().ConvertToHtml(result);

        Assert.StartsWith("<!DOCTYPE", secondHtml);
        Assert.Contains("Twice around the loop", secondHtml);
    }

    [Fact]
    public void HtmlToDocxToHtml_PreservesTheText()
    {
        using var dir = new TempDirectory();
        var intermediate = dir.NewFile(".docx");

        var code = new global::HtmlToDocx.Converter.DocxConverter()
            .ConvertToDocx("<html><body><p>Authored in HTML</p></body></html>", intermediate);
        var html = new global::DocxToHtml.Converter.HtmlConverter().ConvertToHtml(intermediate);

        Assert.Equal(1, code);
        Assert.Contains("Authored in HTML", html);
    }

    [Fact]
    public void UnsupportedSource_IsReportedBeforeAnyDocumentIsWritten()
    {
        using var dir = new TempDirectory();
        var result = dir.NewFile(".docx");

        var html = new global::DocxToHtml.Converter.HtmlConverter().ConvertToHtml(dir.NewFile(".txt"));

        Assert.Equal("Unsupported format", html);
        Assert.False(System.IO.File.Exists(result));
    }
}
