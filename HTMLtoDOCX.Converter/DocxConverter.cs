using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO;
using HtmlToOpenXml;

namespace HtmlToDocx.Converter;

public class DocxConverter
{
    public int ConvertToDocx(string Html, string filename)
    {
        try
        {
            using var generatedDocument = new MemoryStream();

            using (var package = WordprocessingDocument.Create(generatedDocument, WordprocessingDocumentType.Document))
            {
                var mainPart = package.MainDocumentPart;
                if (mainPart == null)
                {
                    mainPart = package.AddMainDocumentPart();
                    new Document(new Body()).Save(mainPart);
                }

                var converter = new HtmlConverter(mainPart);
                converter.ParseHtml(Html);

                mainPart.Document.Save();
            }

            // A package writes its zip structure when it is closed, so the stream only
            // holds a complete document once the block above has ended. Writing the
            // destination afterwards also leaves any previous document in place when the
            // conversion fails.
            File.WriteAllBytes(filename, generatedDocument.ToArray());

            return 1;
        }
        catch
        {
            return -1;
        }

    }

}
