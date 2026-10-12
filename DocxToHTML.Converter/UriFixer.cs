using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace DocxToHtml.Converter;

public static class UriFixer
{
    public static void FixInvalidUri(Stream fs, Func<string, string> invalidUriHandler)
    {

        XNamespace relNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        // The stream belongs to the caller, which decides when it is closed.
        using var za = new ZipArchive(fs, ZipArchiveMode.Update, leaveOpen: true);
        foreach (var entry in za.Entries.ToList())
        {
            // OPC part names are compared case-insensitively, so the casing of the
            // extension cannot decide whether a relationship part is inspected.
            if (!entry.Name.EndsWith(".rels", StringComparison.OrdinalIgnoreCase))
                continue;
            var replaceEntry = false;
            XDocument? entryXDoc;
            using (var entryStream = entry.Open())
            {
                try
                {
                    entryXDoc = XDocument.Load(entryStream);
                    if (entryXDoc.Root != null && entryXDoc.Root.Name.Namespace == relNs)
                    {
                        var urisToCheck = entryXDoc
                            .Descendants(relNs + "Relationship")
                            .Where(r => r.Attribute("TargetMode") != null && (string)r.Attribute("TargetMode") == "External");
                        foreach (var rel in urisToCheck)
                        {
                            var target = (string)rel.Attribute("Target");
                            if (target != null)
                            {
                                try
                                {
                                    _ = new Uri(target);
                                }
                                catch (UriFormatException)
                                {
                                    var newUri = invalidUriHandler(target);
                                    rel.Attribute("Target")!.Value = newUri;
                                    replaceEntry = true;
                                }
                            }
                        }
                    }
                }
                catch (XmlException)
                {
                    continue;
                }
            }
            if (replaceEntry)
            {
                var fullName = entry.FullName;
                entry.Delete();
                var newEntry = za.CreateEntry(fullName);
                using var writer = new StreamWriter(newEntry.Open());
                using var xmlWriter = XmlWriter.Create(writer);
                entryXDoc.WriteTo(xmlWriter);
            }
        }
    }

}