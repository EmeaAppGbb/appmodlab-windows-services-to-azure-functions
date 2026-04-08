using System;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using log4net;

namespace Meridian.ComplianceReporter.Exporters
{
    public class PdfExporter
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(PdfExporter));

        public void ExportToPdf(string content, string filePath)
        {
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Create))
                {
                    var document = new Document(PageSize.A4);
                    PdfWriter.GetInstance(document, fs);
                    document.Open();

                    var font = FontFactory.GetFont(FontFactory.COURIER, 10);
                    var paragraph = new Paragraph(content, font);
                    document.Add(paragraph);

                    document.Close();
                }
                log.Info($"PDF exported successfully: {filePath}");
            }
            catch (Exception ex)
            {
                log.Error($"Error exporting PDF to {filePath}", ex);
                throw;
            }
        }
    }
}
