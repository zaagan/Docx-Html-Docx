using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NotesFor.HtmlToOpenXml;
using System.IO;

namespace HTMLtoDOCX.Converter
{
    public class DocxConverter
    {
        public int ConvertToDocx(string html, string filename)
        {
            try
            {
                if (File.Exists(filename)) File.Delete(filename);

                using (MemoryStream generatedDocument = new MemoryStream())
                {
                    using (WordprocessingDocument package = WordprocessingDocument.Create(generatedDocument, WordprocessingDocumentType.Document))
                    {
                        MainDocumentPart mainPart = package.MainDocumentPart;
                        if (mainPart == null)
                        {
                            mainPart = package.AddMainDocumentPart();
                            new Document(new Body()).Save(mainPart);
                        }

                        HtmlConverter converter = new HtmlConverter(mainPart);
                        converter.ParseHtml(html);

                        mainPart.Document.Save();
                    }

                    File.WriteAllBytes(filename, generatedDocument.ToArray());
                }

                return 1;
            }
            catch 
            {
                return -1;
            }


        }

    }
}
