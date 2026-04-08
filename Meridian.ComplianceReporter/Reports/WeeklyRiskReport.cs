using System;
using System.Configuration;
using System.Data;
using System.IO;
using log4net;
using Meridian.ComplianceReporter.Exporters;
using Meridian.Shared.Data;

namespace Meridian.ComplianceReporter.Reports
{
    public class WeeklyRiskReport
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(WeeklyRiskReport));
        private readonly ExcelExporter excelExporter;
        private readonly string reportOutputPath;

        public WeeklyRiskReport()
        {
            excelExporter = new ExcelExporter();
            reportOutputPath = ConfigurationManager.AppSettings["ReportOutputPath"] ?? @"C:\MeridianData\Reports";
            
            if (!Directory.Exists(reportOutputPath))
            {
                Directory.CreateDirectory(reportOutputPath);
            }
        }

        public void GenerateReport()
        {
            log.Info("Generating weekly risk report...");

            try
            {
                var data = SqlHelper.ExecuteStoredProcedure(StoredProcedures.GetWeeklyRiskData);
                
                var reportDate = DateTime.Now;
                var fileName = $"WeeklyRisk_{reportDate:yyyyMMdd}.xlsx";
                var filePath = Path.Combine(reportOutputPath, fileName);

                excelExporter.ExportToExcel(data, filePath, "Weekly Risk Summary");

                log.Info($"Weekly risk report generated: {fileName}");
            }
            catch (Exception ex)
            {
                log.Error("Error generating weekly risk report", ex);
                throw;
            }
        }
    }
}
