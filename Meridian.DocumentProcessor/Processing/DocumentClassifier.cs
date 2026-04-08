using System;
using System.IO;
using log4net;

namespace Meridian.DocumentProcessor.Processing
{
    public class DocumentClassifier
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(DocumentClassifier));

        public string ClassifyDocument(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var fileName = Path.GetFileName(filePath).ToLowerInvariant();

            if (extension == ".pdf")
            {
                if (fileName.Contains("statement") || fileName.Contains("stmt"))
                    return "AccountStatement";
                else if (fileName.Contains("trade") || fileName.Contains("confirm"))
                    return "TradeConfirmation";
                else if (fileName.Contains("report"))
                    return "ComplianceReport";
                else
                    return "GeneralPDF";
            }
            else if (extension == ".csv")
            {
                if (fileName.Contains("position") || fileName.Contains("holdings"))
                    return "PositionFile";
                else if (fileName.Contains("transaction") || fileName.Contains("trade"))
                    return "TransactionFile";
                else if (fileName.Contains("price") || fileName.Contains("market"))
                    return "MarketDataFile";
                else
                    return "GeneralCSV";
            }
            else if (extension == ".xml")
            {
                return "XMLData";
            }

            log.Warn($"Unknown document type for file: {filePath}");
            return "Unknown";
        }
    }
}
