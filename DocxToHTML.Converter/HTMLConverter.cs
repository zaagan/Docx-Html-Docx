using System.IO;
using System.Linq;
using OpenXmlPowerTools;
using DocumentFormat.OpenXml.Packaging;
using System.Drawing.Imaging;
using System.Xml.Linq;

namespace DocxToHTML.Converter
{

    public class HTMLConverter
    {


        public string ConvertToHtml(string fullFilePath)
        {
            if (string.IsNullOrEmpty(fullFilePath) || Path.GetExtension(fullFilePath) != ".docx")
                return "Unsupported format";

            FileInfo fileInfo = new FileInfo(fullFilePath);

            string htmlText = string.Empty;
            try
            {
                htmlText = ParseDOCX(fileInfo);
            }
            catch (OpenXmlPackageException e)
            {

                if (e.ToString().Contains("Invalid Hyperlink"))
                {
                    using (FileStream fs = new FileStream(fullFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        UriFixer.FixInvalidUri(fs, brokenUri => FixUri(brokenUri));
                    }
                    htmlText = ParseDOCX(fileInfo);
                }
            }


            return htmlText;


        }

        private static string FixUri(string brokenUri)
        {
            string newURI = string.Empty;

            if (brokenUri.Contains("mailto:"))
            {
                int mailToCount = "mailto:".Length;
                brokenUri = brokenUri.Remove(0, mailToCount);
                newURI = brokenUri;
            }
            else
            {
                newURI = " ";
            }
            return newURI;
        }


        private string ParseDOCX(FileInfo fileInfo)
        {

            try
            {
                byte[] byteArray = File.ReadAllBytes(fileInfo.FullName);
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    memoryStream.Write(byteArray, 0, byteArray.Length);

                    using (WordprocessingDocument wDoc = WordprocessingDocument.Open(memoryStream, true))
                    {

                        int imageCounter = 0;

                        var pageTitle = fileInfo.FullName;
                        var part = wDoc.CoreFilePropertiesPart;
                        if (part != null)
                            pageTitle = (string)part.GetXDocument().Descendants(DC.title).FirstOrDefault() ?? fileInfo.FullName;

                        WmlToHtmlConverterSettings settings = new WmlToHtmlConverterSettings()
                        {
                            AdditionalCss = "body { margin: 1cm auto; max-width: 20cm; padding: 0; }",
                            PageTitle = pageTitle,
                            FabricateCssClasses = true,
                            CssClassPrefix = "pt-",
                            RestrictToSupportedLanguages = false,
                            RestrictToSupportedNumberingFormats = false,
                            ImageHandler = imageInfo =>
                            {
                                ++imageCounter;
                                string extension = imageInfo.ContentType.Split('/')[1].ToLower();
                                ImageFormat imageFormat = null;
                                if (extension == "png") imageFormat = ImageFormat.Png;
                                else if (extension == "gif") imageFormat = ImageFormat.Gif;
                                else if (extension == "bmp") imageFormat = ImageFormat.Bmp;
                                else if (extension == "jpeg") imageFormat = ImageFormat.Jpeg;
                                else if (extension == "tiff")
                                {
                                    extension = "gif";
                                    imageFormat = ImageFormat.Gif;
                                }
                                else if (extension == "x-wmf")
                                {
                                    extension = "wmf";
                                    imageFormat = ImageFormat.Wmf;
                                }

                                if (imageFormat == null)
                                    return null;

                                string base64 = null;
                                try
                                {
                                    using (MemoryStream ms = new MemoryStream())
                                    {
                                        imageInfo.Bitmap.Save(ms, imageFormat);
                                        var ba = ms.ToArray();
                                        base64 = System.Convert.ToBase64String(ba);
                                    }
                                }
                                catch (System.Runtime.InteropServices.ExternalException)
                                { return null; }


                                ImageFormat format = imageInfo.Bitmap.RawFormat;
                                ImageCodecInfo codec = ImageCodecInfo.GetImageDecoders().First(c => c.FormatID == format.Guid);
                                string mimeType = codec.MimeType;

                                string imageSource = string.Format("data:{0};base64,{1}", mimeType, base64);

                                XElement img = new XElement(Xhtml.img,
                                    new XAttribute(NoNamespace.src, imageSource),
                                    imageInfo.ImgStyleAttribute,
                                    imageInfo.AltText != null ?
                                        new XAttribute(NoNamespace.alt, imageInfo.AltText) : null);
                                return img;
                            }
                        };

                        XElement htmlElement = WmlToHtmlConverter.ConvertToHtml(wDoc, settings);

                        var html = new XDocument(new XDocumentType("html", null, null, null), htmlElement);
                        var htmlString = html.ToString(SaveOptions.DisableFormatting);
                        return htmlString;
                    }
                }
            }
            catch
            {
                return "File contains corrupt data";
            }

        }


    }
}
