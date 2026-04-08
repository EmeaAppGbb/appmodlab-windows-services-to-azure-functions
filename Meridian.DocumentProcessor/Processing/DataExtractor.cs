using System;
using System.Data.SqlClient;
using log4net;
using Meridian.DocumentProcessor.FileWatcher;
using Meridian.Shared.Data;

namespace Meridian.DocumentProcessor.Processing
{
    public class DataExtractor
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(DataExtractor));
        private readonly PdfParser pdfParser;
        private readonly CsvImporter csvImporter;

        public DataExtractor()
        {
            pdfParser = new PdfParser();
            csvImporter = new CsvImporter();
        }

        public string ExtractData(string filePath, string documentType)
        {
            try
            {
                var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
                string extractedData;

                if (extension == ".pdf")
                {
                    extractedData = pdfParser.ExtractText(filePath);
                }
                else if (extension == ".csv")
                {
                    extractedData = csvImporter.ImportCsv(filePath);
                }
                else
                {
                    extractedData = $"Unsupported file type: {extension}";
                }

                SaveToDatabase(filePath, documentType, extractedData);
                return extractedData;
            }
            catch (Exception ex)
            {
                log.Error($"Error extracting data from {filePath}", ex);
                throw;
            }
        }

        private void SaveToDatabase(string filePath, string documentType, string extractedData)
        {
            try
            {
                var fileName = System.IO.Path.GetFileName(filePath);
                var parameters = new[]
                {
                    new SqlParameter("@FileName", fileName),
                    new SqlParameter("@DocumentType", documentType),
                    new SqlParameter("@ReceivedDate", DateTime.Now),
                    new SqlParameter("@Status", "Processed"),
                    new SqlParameter("@ClientId", 1),
                    new SqlParameter("@ExtractedData", extractedData.Length > 4000 ? extractedData.Substring(0, 4000) : extractedData)
                };

                SqlHelper.ExecuteStoredProcedureNonQuery(StoredProcedures.InsertDocument, parameters);
                log.Info($"Document saved to database: {fileName}");
            }
            catch (Exception ex)
            {
                log.Error("Error saving document to database", ex);
            }
        }
    }
}
