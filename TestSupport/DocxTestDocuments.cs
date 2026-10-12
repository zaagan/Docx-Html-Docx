using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Docx.TestSupport;

/// <summary>
/// Builds the .docx fixtures the converter tests run against.
/// </summary>
internal static class DocxTestDocuments
{
    private const string RelationshipsNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string HyperlinkRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink";

    /// <summary>
    /// Writes a document that carries the parts Word always emits (styles, numbering,
    /// settings). OpenXmlPowerTools' WmlToHtmlConverter dereferences all three
    /// unconditionally, so a fixture without them exercises an upstream crash rather than
    /// this repository's code.
    /// </summary>
    public static void Write(string path, params string[] paragraphs) => WriteWithTitle(path, null, paragraphs);

    /// <summary>As <see cref="Write(string, string[])"/>, also setting the core-properties title.</summary>
    public static void WriteWithTitle(string path, string? title, params string[] paragraphs)
    {
        using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);

        var mainPart = document.AddMainDocumentPart();
        var body = new Body();
        foreach (var text in paragraphs.DefaultIfEmpty("Fixture paragraph"))
            body.AppendChild(new Paragraph(new Run(new Text(text))));
        mainPart.Document = new Document(body);

        var styles = mainPart.AddNewPart<StyleDefinitionsPart>();
        styles.Styles = new Styles(
            new DocDefaults(
                new RunPropertiesDefault(new RunPropertiesBaseStyle()),
                new ParagraphPropertiesDefault(new ParagraphPropertiesBaseStyle())));
        styles.Styles.Save();

        var numbering = mainPart.AddNewPart<NumberingDefinitionsPart>();
        numbering.Numbering = new Numbering();
        numbering.Numbering.Save();

        var settings = mainPart.AddNewPart<DocumentSettingsPart>();
        settings.Settings = new Settings();
        settings.Settings.Save();

        if (title != null)
            document.PackageProperties.Title = title;

        mainPart.Document.Save();
    }

    /// <summary>
    /// Writes a structurally valid document that has only a main part, i.e. no styles,
    /// numbering or settings.
    /// </summary>
    public static void WriteWithoutStyles(string path, string text = "Minimal document")
    {
        using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document(new Body(new Paragraph(new Run(new Text(text)))));
        mainPart.Document.Save();
    }

    /// <summary>Adds an external relationship with the given raw target to word/_rels/document.xml.rels.</summary>
    public static void AddExternalRelationship(string docxPath, string target, string id = "rIdExternalFixture")
    {
        using var archive = ZipFile.Open(docxPath, ZipArchiveMode.Update);
        var entry = archive.Entries.First(e => e.FullName == "word/_rels/document.xml.rels");

        string xml;
        using (var reader = new StreamReader(entry.Open()))
            xml = reader.ReadToEnd();

        var relationship = string.Format(
            "<Relationship Id=\"{0}\" Type=\"{1}\" Target=\"{2}\" TargetMode=\"External\" />",
            id, HyperlinkRelationshipType, target);
        xml = xml.Replace("</Relationships>", relationship + "</Relationships>");

        var fullName = entry.FullName;
        entry.Delete();
        var replacement = archive.CreateEntry(fullName);
        using var writer = new StreamWriter(replacement.Open(), new UTF8Encoding(false));
        writer.Write(xml);
    }

    /// <summary>Writes bytes that are not a zip archive at all.</summary>
    public static void WriteCorrupt(string path) =>
        File.WriteAllBytes(path, Encoding.ASCII.GetBytes("this is not an OPC package"));

    /// <summary>
    /// Builds a zip archive in memory from the given entries, as a stand-in for an OPC
    /// package when only the relationship parts matter.
    /// </summary>
    public static MemoryStream CreateArchive(params (string Name, string Content)[] entries)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                var entry = archive.CreateEntry(name);
                using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
                writer.Write(content);
            }
        }

        stream.Position = 0;
        return stream;
    }

    /// <summary>A .rels document declaring one relationship per supplied target.</summary>
    public static string RelationshipsXml(params (string Target, string? TargetMode)[] relationships)
    {
        var builder = new StringBuilder();
        builder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        builder.Append("<Relationships xmlns=\"").Append(RelationshipsNamespace).Append("\">");
        var id = 1;
        foreach (var (target, targetMode) in relationships)
        {
            builder.Append("<Relationship Id=\"rId").Append(id++).Append('"')
                .Append(" Type=\"").Append(HyperlinkRelationshipType).Append('"')
                .Append(" Target=\"").Append(target).Append('"');
            if (targetMode != null)
                builder.Append(" TargetMode=\"").Append(targetMode).Append('"');
            builder.Append(" />");
        }

        builder.Append("</Relationships>");
        return builder.ToString();
    }

    /// <summary>Reads the raw text of one entry of a zip archive held in a stream.</summary>
    public static string ReadArchiveEntry(MemoryStream archiveStream, string entryName)
    {
        // ToArray() keeps working on a closed MemoryStream, so the repaired bytes can be
        // inspected whether or not the code under test disposed the stream it was given.
        using var copy = new MemoryStream(archiveStream.ToArray());
        using var archive = new ZipArchive(copy, ZipArchiveMode.Read);
        var entry = archive.Entries.First(e => string.Equals(e.FullName, entryName, StringComparison.OrdinalIgnoreCase));
        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd();
    }
}
