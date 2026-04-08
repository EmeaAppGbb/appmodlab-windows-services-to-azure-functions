using System;
using System.Configuration;
using System.Data;
using System.IO;
using log4net;
using Meridian.ComplianceReporter.Exporters;
using Meridian.Shared.Data;

namespace Meridian.ComplianceReporter.Reports
{
    public class MonthlyAuditReport
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MonthlyAuditReport));
        private readonly PdfExporter pdfExporter;
        private readonly ExcelExporter excelExporter;
        private readonly string reportOutputPath;

        public MonthlyAuditReport()
        {
            pdfExporter = new PdfExporter();
            excelExporter = new ExcelExporter();
            reportOutputPath = ConfigurationManager.AppSettings["ReportOutputPath"] ?? @"C:\MeridianData\Reports";
            
            if (!Directory.Exists(reportOutputPath))
            {
                Directory.CreateDirectory(reportOutputPath);
            }
        }

        public void GenerateReport()
        {
            log.Info("Generating monthly audit report...");

            try
            {
                var data = SqlHelper.ExecuteStoredProcedure(StoredProcedures.GetMonthlyAuditData);
                
                var reportDate = DateTime.Now;
                var pdfFileName = $"MonthlyAudit_{reportDate:yyyyMM}.pdf";
                var xlsxFileName = $"MonthlyAudit_{reportDate:yyyyMM}.xlsx";
                var pdfPath = Path.Combine(reportOutputPath, pdfFileName);
                var xlsxPath = Path.Combine(reportOutputPath, xlsxFileName);

                var reportContent = BuildAuditContent(data, reportDate);
                pdfExporter.ExportToPdf(reportContent, pdfPath);
                excelExporter.ExportToExcel(data, xlsxPath, "Monthly Audit Trail");

                log.Info($"Monthly audit report generated: {pdfFileName} and {xlsxFileName}");
            }
            catch (Exception ex)
            {
                log.Error("Error generating monthly audit report", ex);
                throw;
            }
        }

        private string BuildAuditContent(DataTable data, DateTime reportDate)
        {
            var content = $@"
MERIDIAN CAPITAL ADVISORS
Monthly Audit Trail Report
Report Period: {reportDate:MMMM yyyy}
Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

========================================

AUDIT SUMMARY
Total Audit Records: {data.Rows.Count}

This report contains a comprehensive audit trail of all compliance-related
activities for the specified reporting period.

REGULATORY COMPLIANCE
All activities documented in this report comply with applicable financial
regulations including SEC Rule 17a-4, FINRA regulations, and internal
compliance policies.
";
            return content;
        }
    }
}
