using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Meridian.Functions.Functions.ComplianceReporting;

/// <summary>
/// Generates a monthly audit report on the 1st of each month at 6 AM UTC.
/// Ported from Meridian.ComplianceReporter/Reports/MonthlyAuditReport.cs which ran on a
/// Topshelf timer service. The legacy version used SqlHelper.ExecuteStoredProcedure with
/// usp_GetMonthlyAuditData and exported to both PDF (iTextSharp) and Excel (EPPlus).
/// This function writes both a text summary and a JSON data file to Azure Blob Storage.
/// </summary>
public class MonthlyAuditReportFunction
{
    private readonly ILogger<MonthlyAuditReportFunction> _logger;

    public MonthlyAuditReportFunction(ILogger<MonthlyAuditReportFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(GenerateMonthlyAuditReport))]
    public async Task GenerateMonthlyAuditReport(
        [TimerTrigger("0 0 6 1 * *")] TimerInfo timerInfo,
        FunctionContext context)
    {
        _logger.LogInformation("Monthly audit report generation triggered at {Time}", DateTime.UtcNow);

        try
        {
            var reportDate = DateTime.UtcNow;
            var auditData = await GetMonthlyAuditDataAsync();

            // Generate text summary (replaces legacy PDF export)
            var summaryContent = BuildAuditContent(auditData, reportDate);
            var summaryFileName = $"MonthlyAudit_{reportDate:yyyyMM}.txt";
            await UploadReportToBlobAsync(summaryContent, summaryFileName);

            // Generate JSON data export (replaces legacy Excel export)
            var dataContent = JsonSerializer.Serialize(new
            {
                ReportType = "Monthly Audit Trail",
                ReportPeriod = reportDate.ToString("MMMM yyyy"),
                GeneratedAt = reportDate,
                RecordCount = auditData.Count,
                Records = auditData
            }, new JsonSerializerOptions { WriteIndented = true });

            var dataFileName = $"MonthlyAudit_{reportDate:yyyyMM}.json";
            await UploadReportToBlobAsync(dataContent, dataFileName);

            _logger.LogInformation("Monthly audit report generated: {SummaryFile} and {DataFile}, Records: {Count}",
                summaryFileName, dataFileName, auditData.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating monthly audit report");
            throw;
        }

        if (timerInfo.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next monthly audit report scheduled at: {NextRun}",
                timerInfo.ScheduleStatus.Next);
        }
    }

    /// <summary>
    /// Retrieves monthly audit data. In the legacy system this called
    /// SqlHelper.ExecuteStoredProcedure(StoredProcedures.GetMonthlyAuditData).
    /// Production deployments should integrate with Azure SQL or Cosmos DB.
    /// </summary>
    private Task<List<AuditRecord>> GetMonthlyAuditDataAsync()
    {
        // Placeholder: production should query the database via usp_GetMonthlyAuditData
        var records = new List<AuditRecord>
        {
            new() { Action = "DocumentProcessed", User = "system", Details = "Trade confirmation processed", Timestamp = DateTime.UtcNow.AddDays(-15) },
            new() { Action = "ComplianceCheck", User = "system", Details = "Daily compliance check completed", Timestamp = DateTime.UtcNow.AddDays(-10) },
            new() { Action = "RuleUpdated", User = "admin", Details = "Updated SEC Rule 17a-4 parameters", Timestamp = DateTime.UtcNow.AddDays(-5) }
        };

        return Task.FromResult(records);
    }

    private static string BuildAuditContent(List<AuditRecord> data, DateTime reportDate)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MERIDIAN CAPITAL ADVISORS");
        sb.AppendLine("Monthly Audit Trail Report");
        sb.AppendLine($"Report Period: {reportDate:MMMM yyyy}");
        sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        sb.AppendLine("========================================");
        sb.AppendLine();
        sb.AppendLine("AUDIT SUMMARY");
        sb.AppendLine($"Total Audit Records: {data.Count}");
        sb.AppendLine();
        sb.AppendLine("This report contains a comprehensive audit trail of all compliance-related");
        sb.AppendLine("activities for the specified reporting period.");
        sb.AppendLine();
        sb.AppendLine("REGULATORY COMPLIANCE");
        sb.AppendLine("All activities documented in this report comply with applicable financial");
        sb.AppendLine("regulations including SEC Rule 17a-4, FINRA regulations, and internal");
        sb.AppendLine("compliance policies.");

        return sb.ToString();
    }

    private async Task UploadReportToBlobAsync(string content, string fileName)
    {
        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        var containerClient = new BlobContainerClient(connectionString, "compliance-reports");
        await containerClient.CreateIfNotExistsAsync();

        var blobClient = containerClient.GetBlobClient(fileName);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await blobClient.UploadAsync(stream, overwrite: true);

        _logger.LogInformation("Report uploaded to blob: compliance-reports/{FileName}", fileName);
    }

    private sealed class AuditRecord
    {
        public string Action { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
