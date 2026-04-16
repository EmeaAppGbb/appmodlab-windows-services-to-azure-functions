using System.Text;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Meridian.Functions.Functions.ComplianceReporting;

/// <summary>
/// Generates a daily compliance report at 6 PM UTC.
/// Ported from Meridian.ComplianceReporter/Reports/DailyComplianceReport.cs which ran on a
/// Topshelf timer service. The legacy version used SqlHelper.ExecuteStoredProcedure with
/// usp_GetDailyComplianceData and exported to a local PDF via iTextSharp. This function
/// writes the report content to Azure Blob Storage instead.
/// </summary>
public class DailyComplianceReportFunction
{
    private readonly ILogger<DailyComplianceReportFunction> _logger;

    public DailyComplianceReportFunction(ILogger<DailyComplianceReportFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(GenerateDailyComplianceReport))]
    public async Task GenerateDailyComplianceReport(
        [TimerTrigger("0 0 18 * * *")] TimerInfo timerInfo,
        FunctionContext context)
    {
        _logger.LogInformation("Daily compliance report generation triggered at {Time}", DateTime.UtcNow);

        try
        {
            var reportDate = DateTime.UtcNow;
            var reportData = await GetDailyComplianceDataAsync();
            var reportContent = BuildReportContent(reportData, reportDate);

            var fileName = $"DailyCompliance_{reportDate:yyyyMMdd}.txt";
            await UploadReportToBlobAsync(reportContent, fileName);

            _logger.LogInformation("Daily compliance report generated: {FileName}, Records: {Count}",
                fileName, reportData.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating daily compliance report");
            throw;
        }

        if (timerInfo.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next daily compliance report scheduled at: {NextRun}",
                timerInfo.ScheduleStatus.Next);
        }
    }

    /// <summary>
    /// Retrieves daily compliance data. In the legacy system this called
    /// SqlHelper.ExecuteStoredProcedure(StoredProcedures.GetDailyComplianceData).
    /// Production deployments should integrate with Azure SQL or Cosmos DB.
    /// </summary>
    private Task<List<ComplianceRecord>> GetDailyComplianceDataAsync()
    {
        // Placeholder: production should query the database via usp_GetDailyComplianceData
        var records = new List<ComplianceRecord>
        {
            new() { FileName = "TradeConfirmation_001.pdf", DocumentType = "TradeConfirmation", Status = "Passed" },
            new() { FileName = "AccountStatement_042.csv", DocumentType = "AccountStatement", Status = "Passed" },
            new() { FileName = "PositionFile_099.xml", DocumentType = "PositionFile", Status = "Failed" }
        };

        return Task.FromResult(records);
    }

    private static string BuildReportContent(List<ComplianceRecord> data, DateTime reportDate)
    {
        var passedCount = data.Count(r => r.Status == "Passed");
        var failedCount = data.Count(r => r.Status == "Failed");

        var sb = new StringBuilder();
        sb.AppendLine("MERIDIAN CAPITAL ADVISORS");
        sb.AppendLine("Daily Compliance Report");
        sb.AppendLine($"Report Date: {reportDate:MMMM dd, yyyy}");
        sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        sb.AppendLine("========================================");
        sb.AppendLine();
        sb.AppendLine("SUMMARY");
        sb.AppendLine($"Total Documents Processed: {data.Count}");
        sb.AppendLine($"Compliance Checks Passed: {passedCount}");
        sb.AppendLine($"Compliance Checks Failed: {failedCount}");
        sb.AppendLine();
        sb.AppendLine("DETAILS");

        foreach (var record in data)
        {
            sb.AppendLine();
            sb.AppendLine($"Document: {record.FileName}");
            sb.AppendLine($"Type: {record.DocumentType}");
            sb.AppendLine($"Status: {record.Status}");
            sb.AppendLine("---");
        }

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

    private sealed class ComplianceRecord
    {
        public string FileName { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
