using System;
using System.IO;
using System.Text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using log4net;

namespace Meridian.DocumentProcessor.FileWatcher
{
    public class PdfParser
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(PdfParser));

        public string ExtractText(string pdfPath)
        {
            try
            {
                var text = new StringBuilder();
                using (PdfReader reader = new PdfReader(pdfPath))
                {
                    for (int page = 1; page <= reader.NumberOfPages; page++)
                    {
                        ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                        string pageText = PdfTextExtractor.GetTextFromPage(reader, page, strategy);
                        text.Append(pageText);
                        text.Append("\n");
                    }
                }
                log.Info($"Extracted {text.Length} characters from PDF: {pdfPath}");
                return text.ToString();
            }
            catch (Exception ex)
            {
                log.Error($"Error extracting text from PDF: {pdfPath}", ex);
                return string.Empty;
            }
        }
    }
}
