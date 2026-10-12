using System;
using System.IO;
using System.Linq;
using OpenXmlPowerTools;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Drawing.Imaging;
using System.Xml.Linq;

namespace DocxToHtml.Converter;

public class HtmlConverter
{


    private const string UnsupportedFormatMessage = "Unsupported format";
    private const string CorruptDataMessage = "File contains corrupt data";

    public string ConvertToHtml(string fullFilePath)
    {
        // Extensions are case-insensitive on the file systems .docx lives on, so the
        // casing of the name cannot decide whether a document is convertible.
        if (string.IsNullOrEmpty(fullFilePath)
            || !string.Equals(Path.GetExtension(fullFilePath), ".docx", StringComparison.OrdinalIgnoreCase))
            return UnsupportedFormatMessage;

        var fileInfo = new FileInfo(fullFilePath);

        try
        {
            return ParseDocx(fileInfo);
        }
        catch (OpenXmlPackageException e) when (e.ToString().Contains("Invalid Hyperlink"))
        {
            try
            {
                // Rewrite the relationship targets the framework rejects, then convert
                // the repaired document.
                using (var fs = new FileStream(fullFilePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    UriFixer.FixInvalidUri(fs, FixUri);
                }

                return ParseDocx(fileInfo);
            }
            catch
            {
                return CorruptDataMessage;
            }
        }
        catch
        {
            // Every failure is reported through the return value, and never as an empty
            // string: a caller cannot tell empty HTML from an unreported error.
            return CorruptDataMessage;
        }
    }

    internal static string FixUri(string brokenUri)
    {
        const string mailToScheme = "mailto:";

        // Only a leading scheme can be stripped: "mailto:" anywhere else in the target is
        // part of the text, and removing a fixed number of characters from it would leave
        // a truncated address behind. Schemes are case-insensitive (RFC 3986 section 3.1).
        if (brokenUri.StartsWith(mailToScheme, StringComparison.OrdinalIgnoreCase))
            return brokenUri.Substring(mailToScheme.Length);

        return " ";
    }


    /// <summary>
    /// Supplies the style, numbering and settings parts that the formatting assembler
    /// reads. All three are optional in OPC, but the assembler dereferences them without
    /// checking, so a document that omits them cannot be converted at all. Empty parts
    /// are what the assembler should have assumed; they are added to the in-memory copy
    /// only, so the document on disk is never modified.
    /// </summary>
    private static void EnsureFormattingParts(WordprocessingDocument wDoc)
    {
        var mainPart = wDoc.MainDocumentPart;
        if (mainPart == null)
            return;

        if (mainPart.StyleDefinitionsPart == null)
        {
            var styles = mainPart.AddNewPart<StyleDefinitionsPart>();
            styles.Styles = new Styles();
            styles.Styles.Save();
        }

        if (mainPart.NumberingDefinitionsPart == null)
        {
            var numbering = mainPart.AddNewPart<NumberingDefinitionsPart>();
            numbering.Numbering = new Numbering();
            numbering.Numbering.Save();
        }

        if (mainPart.DocumentSettingsPart == null)
        {
            var settings = mainPart.AddNewPart<DocumentSettingsPart>();
            settings.Settings = new Settings();
            settings.Settings.Save();
        }
    }

    private string ParseDocx(FileInfo fileInfo)
    {
        var byteArray = File.ReadAllBytes(fileInfo.FullName);
        using var memoryStream = new MemoryStream();
        memoryStream.Write(byteArray, 0, byteArray.Length);

        using var wDoc = WordprocessingDocument.Open(memoryStream, true);
        EnsureFormattingParts(wDoc);

        var pageTitle = fileInfo.FullName;
        var part = wDoc.CoreFilePropertiesPart;
        if (part != null)
            pageTitle = (string)part.GetXDocument().Descendants(DC.title).FirstOrDefault() ?? fileInfo.FullName;

        var settings = new WmlToHtmlConverterSettings()
        {
            AdditionalCss = "body { margin: 1cm auto; max-width: 20cm; padding: 0; }",
            PageTitle = pageTitle,
            FabricateCssClasses = true,
            CssClassPrefix = "pt-",
            RestrictToSupportedLanguages = false,
            RestrictToSupportedNumberingFormats = false,
            ImageHandler = imageInfo =>
            {
                var extension = imageInfo.ContentType.Split('/')[1].ToLower();
                ImageFormat? imageFormat = null;
                if (extension == "png") imageFormat = ImageFormat.Png;
                else if (extension == "gif") imageFormat = ImageFormat.Gif;
                else if (extension == "bmp") imageFormat = ImageFormat.Bmp;
                else if (extension == "jpeg") imageFormat = ImageFormat.Jpeg;
                else if (extension == "tiff")
                {
                    imageFormat = ImageFormat.Gif;
                }
                else if (extension == "x-wmf")
                {
                    imageFormat = ImageFormat.Wmf;
                }

                if (imageFormat == null)
                    return null;

                string? base64;
                try
                {
                    using var ms = new MemoryStream();
                    imageInfo.Bitmap.Save(ms, imageFormat);
                    var ba = ms.ToArray();
                    base64 = System.Convert.ToBase64String(ba);
                }
                catch (System.Runtime.InteropServices.ExternalException)
                { return null; }


                var format = imageInfo.Bitmap.RawFormat;
                var codec = ImageCodecInfo.GetImageDecoders().First(c => c.FormatID == format.Guid);
                var mimeType = codec.MimeType;

                var imageSource = string.Format("data:{0};base64,{1}", mimeType, base64);

                var img = new XElement(Xhtml.img,
                    new XAttribute(NoNamespace.src, imageSource),
                    imageInfo.ImgStyleAttribute,
                    imageInfo.AltText != null ?
                        new XAttribute(NoNamespace.alt, imageInfo.AltText) : null);
                return img;
            }
        };

        var HtmlElement = WmlToHtmlConverter.ConvertToHtml(wDoc, settings);

        var Html = new XDocument(new XDocumentType("Html", null, null, null), HtmlElement);
        var HtmlString = Html.ToString(SaveOptions.DisableFormatting);
        return HtmlString;
    }


}