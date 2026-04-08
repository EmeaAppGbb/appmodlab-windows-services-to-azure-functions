using System;
using System.Configuration;
using System.Data;
using System.IO;
using log4net;
using Meridian.ComplianceReporter.Exporters;
using Meridian.Shared.Data;

namespace Meridian.ComplianceReporter.Reports
{
    public class DailyComplianceReport
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(DailyComplianceReport));
        private readonly PdfExporter pdfExporter;
        private readonly string reportOutputPath;

        public DailyComplianceReport()
        {
            pdfExporter = new PdfExporter();
            reportOutputPath = ConfigurationManager.AppSettings["ReportOutputPath"] ?? @"C:\MeridianData\Reports";
            
            if (!Directory.Exists(reportOutputPath))
            {
                Directory.CreateDirectory(reportOutputPath);
            }
        }

        public void GenerateReport()
        {
            log.Info("Generating daily compliance report...");

            try
            {
                var data = SqlHelper.ExecuteStoredProcedure(StoredProcedures.GetDailyComplianceData);
                
                var reportDate = DateTime.Now;
                var fileName = $"DailyCompliance_{reportDate:yyyyMMdd}.pdf";
                var filePath = Path.Combine(reportOutputPath, fileName);

                var reportContent = BuildReportContent(data, reportDate);
                pdfExporter.ExportToPdf(reportContent, filePath);

                log.Info($"Daily compliance report generated: {fileName}");
            }
            catch (Exception ex)
            {
                log.Error("Error generating daily compliance report", ex);
                throw;
            }
        }

        private string BuildReportContent(DataTable data, DateTime reportDate)
        {
            var content = $@"
MERIDIAN CAPITAL ADVISORS
Daily Compliance Report
Report Date: {reportDate:MMMM dd, yyyy}
Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

========================================

SUMMARY
Total Documents Processed: {data.Rows.Count}
Compliance Checks Passed: {CountPassed(data)}
Compliance Checks Failed: {CountFailed(data)}

DETAILS
";
            foreach (DataRow row in data.Rows)
            {
                content += $"\nDocument: {row["FileName"]}\n";
                content += $"Type: {row["DocumentType"]}\n";
                content += $"Status: {row["Status"]}\n";
                content += "---\n";
            }

            return content;
        }

        private int CountPassed(DataTable data)
        {
            int count = 0;
            foreach (DataRow row in data.Rows)
            {
                if (row["Status"].ToString() == "Passed")
                    count++;
            }
            return count;
        }

        private int CountFailed(DataTable data)
        {
            int count = 0;
            foreach (DataRow row in data.Rows)
            {
                if (row["Status"].ToString() == "Failed")
                    count++;
            }
            return count;
        }
    }
}
