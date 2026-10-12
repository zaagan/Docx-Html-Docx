using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Docx.TestSupport;
using DocxToHtml.Converter;

namespace DocxToHtml.Converter.Tests;

public class UriFixerTests
{
    private const string InvalidTarget = "mailto:first@example.com second@example.com";
    private const string ValidTarget = "https://example.com/page";

    [Fact]
    public void FixInvalidUri_InvalidExternalTarget_IsReplacedWithTheHandlerResult()
    {
        using var archive = DocxTestDocuments.CreateArchive(
            ("word/_rels/document.xml.rels", DocxTestDocuments.RelationshipsXml((InvalidTarget, "External"))));

        UriFixer.FixInvalidUri(archive, _ => "https://replacement.example");

        var rels = DocxTestDocuments.ReadArchiveEntry(archive, "word/_rels/document.xml.rels");
        Assert.Contains("https://replacement.example", rels);
        Assert.DoesNotContain("second@example.com", rels);
    }

    [Fact]
    public void FixInvalidUri_InvalidExternalTarget_PassesTheBrokenTargetToTheHandler()
    {
        var seen = new List<string>();
        using var archive = DocxTestDocuments.CreateArchive(
            ("word/_rels/document.xml.rels", DocxTestDocuments.RelationshipsXml((InvalidTarget, "External"))));

        UriFixer.FixInvalidUri(archive, broken => { seen.Add(broken); return " "; });

        Assert.Equal(new[] { InvalidTarget }, seen);
    }

    [Fact]
    public void FixInvalidUri_ValidExternalTarget_IsLeftUntouched()
    {
        using var archive = DocxTestDocuments.CreateArchive(
            ("word/_rels/document.xml.rels", DocxTestDocuments.RelationshipsXml((ValidTarget, "External"))));

        UriFixer.FixInvalidUri(archive, _ => throw new InvalidOperationException("handler must not run for a valid target"));

        Assert.Contains(ValidTarget, DocxTestDocuments.ReadArchiveEntry(archive, "word/_rels/document.xml.rels"));
    }

    [Fact]
    public void FixInvalidUri_TargetWithoutExternalTargetMode_IsLeftUntouched()
    {
        // An internal target is a part name, not a URI, so it is none of this method's business.
        using var archive = DocxTestDocuments.CreateArchive(
            ("word/_rels/document.xml.rels", DocxTestDocuments.RelationshipsXml(("/word/styles.xml", null))));

        UriFixer.FixInvalidUri(archive, _ => throw new InvalidOperationException("handler must not run for an internal target"));

        Assert.Contains("/word/styles.xml", DocxTestDocuments.ReadArchiveEntry(archive, "word/_rels/document.xml.rels"));
    }

    [Fact]
    public void FixInvalidUri_MultipleInvalidTargets_AreAllReplaced()
    {
        using var archive = DocxTestDocuments.CreateArchive(
            ("word/_rels/document.xml.rels", DocxTestDocuments.RelationshipsXml(
                (InvalidTarget, "External"),
                (ValidTarget, "External"),
                ("mailto:third@example.com fourth@example.com", "External"))));

        var replacements = 0;
        UriFixer.FixInvalidUri(archive, _ => { replacements++; return "https://replacement.example"; });

        Assert.Equal(2, replacements);
        var rels = DocxTestDocuments.ReadArchiveEntry(archive, "word/_rels/document.xml.rels");
        Assert.DoesNotContain("second@example.com", rels);
        Assert.DoesNotContain("fourth@example.com", rels);
        Assert.Contains(ValidTarget, rels);
    }

    [Fact]
    public void FixInvalidUri_EntryThatIsNotARelationshipPart_IsLeftUntouched()
    {
        const string documentXml = "<document><target>mailto:first@example.com second@example.com</target></document>";
        using var archive = DocxTestDocuments.CreateArchive(("word/document.xml", documentXml));

        UriFixer.FixInvalidUri(archive, _ => throw new InvalidOperationException("handler must not run for a non-rels part"));

        Assert.Equal(documentXml, DocxTestDocuments.ReadArchiveEntry(archive, "word/document.xml"));
    }

    [Fact]
    public void FixInvalidUri_RelsExtensionInAnyCase_IsProcessed()
    {
        // OPC part names are compared case-insensitively, so the casing of the extension
        // must not decide whether a relationship part is inspected.
        using var archive = DocxTestDocuments.CreateArchive(
            ("word/_rels/document.xml.RELS", DocxTestDocuments.RelationshipsXml((InvalidTarget, "External"))));

        UriFixer.FixInvalidUri(archive, _ => "https://replacement.example");

        Assert.Contains("https://replacement.example",
            DocxTestDocuments.ReadArchiveEntry(archive, "word/_rels/document.xml.RELS"));
    }

    [Fact]
    public void FixInvalidUri_RelationshipPartWithMalformedXml_IsSkippedWithoutThrowing()
    {
        using var archive = DocxTestDocuments.CreateArchive(
            ("word/_rels/broken.xml.rels", "<Relationships><unclosed>"),
            ("word/_rels/document.xml.rels", DocxTestDocuments.RelationshipsXml((InvalidTarget, "External"))));

        UriFixer.FixInvalidUri(archive, _ => "https://replacement.example");

        // The malformed part must not stop the valid one from being repaired.
        Assert.Contains("https://replacement.example",
            DocxTestDocuments.ReadArchiveEntry(archive, "word/_rels/document.xml.rels"));
    }

    [Fact]
    public void FixInvalidUri_RelationshipPartInForeignNamespace_IsLeftUntouched()
    {
        const string foreign = "<Relationships xmlns=\"urn:not-opc\">"
            + "<Relationship Id=\"rId1\" Target=\"mailto:first@example.com second@example.com\" TargetMode=\"External\" />"
            + "</Relationships>";
        using var archive = DocxTestDocuments.CreateArchive(("word/_rels/document.xml.rels", foreign));

        UriFixer.FixInvalidUri(archive, _ => throw new InvalidOperationException("handler must not run for a foreign namespace"));

        Assert.Equal(foreign, DocxTestDocuments.ReadArchiveEntry(archive, "word/_rels/document.xml.rels"));
    }

    [Fact]
    public void FixInvalidUri_RepairedArchive_RemainsReadableXml()
    {
        using var archive = DocxTestDocuments.CreateArchive(
            ("word/_rels/document.xml.rels", DocxTestDocuments.RelationshipsXml((InvalidTarget, "External"))));

        UriFixer.FixInvalidUri(archive, _ => "https://replacement.example");

        var rels = DocxTestDocuments.ReadArchiveEntry(archive, "word/_rels/document.xml.rels");
        var document = XDocument.Parse(rels);
        Assert.NotNull(document.Root);
        Assert.Equal("Relationships", document.Root!.Name.LocalName);
    }

    [Fact]
    public void FixInvalidUri_ArchiveWithoutRelationshipParts_LeavesTheArchiveIntact()
    {
        using var archive = DocxTestDocuments.CreateArchive(("word/document.xml", "<document />"));

        UriFixer.FixInvalidUri(archive, _ => " ");

        Assert.Equal("<document />", DocxTestDocuments.ReadArchiveEntry(archive, "word/document.xml"));
    }

    [Fact]
    public void FixInvalidUri_LeavesTheCallerStreamOpen()
    {
        using var archive = DocxTestDocuments.CreateArchive(
            ("word/_rels/document.xml.rels", DocxTestDocuments.RelationshipsXml((InvalidTarget, "External"))));

        UriFixer.FixInvalidUri(archive, _ => "https://replacement.example");

        // The stream belongs to the caller, which has to be able to go on using it.
        Assert.True(archive.CanRead, "the stream passed in must not be disposed by the fixer");
        Assert.True(archive.Length > 0);
    }

    [Fact]
    public void FixInvalidUri_EmptyArchive_DoesNotThrow()
    {
        using var archive = DocxTestDocuments.CreateArchive();

        UriFixer.FixInvalidUri(archive, _ => " ");

        Assert.NotEmpty(archive.ToArray());
    }
}
