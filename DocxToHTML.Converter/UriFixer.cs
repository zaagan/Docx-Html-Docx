using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace DocxToHTML.Converter
{
    public static class UriFixer
    {
        public static void FixInvalidUri(Stream fs, Func<string, string> invalidUriHandler)
        {

            XNamespace relNs = "http://schemas.openxmlformats.org/package/2006/relationships";
            using (ZipArchive za = new ZipArchive(fs, ZipArchiveMode.Update))
            {
                foreach (var entry in za.Entries.ToList())
                {
                    if (!entry.Name.EndsWith(".rels"))
                        continue;
                    bool replaceEntry = false;
                    XDocument entryXDoc = null;
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
                                            Uri uri = new Uri(target);
                                        }
                                        catch (UriFormatException)
                                        {
                                            string newUri = invalidUriHandler(target);
                                            rel.Attribute("Target").Value = newUri;
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
                        using (StreamWriter writer = new StreamWriter(newEntry.Open()))
                        using (XmlWriter xmlWriter = XmlWriter.Create(writer))
                        {
                            entryXDoc.WriteTo(xmlWriter);
                        }
                    }
                }
            }
        }

    }

}
