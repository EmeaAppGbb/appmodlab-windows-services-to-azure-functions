using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Meridian.Functions.Functions.ComplianceReporting;

/// <summary>
/// Generates a weekly risk report every Friday at 9 AM UTC.
/// Ported from Meridian.ComplianceReporter/Reports/WeeklyRiskReport.cs which ran on a
/// Topshelf timer service. The legacy version used SqlHelper.ExecuteStoredProcedure with
/// usp_GetWeeklyRiskData and exported to an Excel file via EPPlus. This function
/// writes a JSON-formatted report to Azure Blob Storage.
/// </summary>
public class WeeklyRiskReportFunction
{
    private readonly ILogger<WeeklyRiskReportFunction> _logger;

    public WeeklyRiskReportFunction(ILogger<WeeklyRiskReportFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(GenerateWeeklyRiskReport))]
    public async Task GenerateWeeklyRiskReport(
        [TimerTrigger("0 0 9 * * 5")] TimerInfo timerInfo,
        FunctionContext context)
    {
        _logger.LogInformation("Weekly risk report generation triggered at {Time}", DateTime.UtcNow);

        try
        {
            var reportDate = DateTime.UtcNow;
            var riskData = await GetWeeklyRiskDataAsync();

            var fileName = $"WeeklyRisk_{reportDate:yyyyMMdd}.json";
            var reportContent = JsonSerializer.Serialize(new
            {
                ReportType = "Weekly Risk Summary",
                GeneratedAt = reportDate,
                RecordCount = riskData.Count,
                Records = riskData
            }, new JsonSerializerOptions { WriteIndented = true });

            await UploadReportToBlobAsync(reportContent, fileName);

            _logger.LogInformation("Weekly risk report generated: {FileName}, Records: {Count}",
                fileName, riskData.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating weekly risk report");
            throw;
        }

        if (timerInfo.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next weekly risk report scheduled at: {NextRun}",
                timerInfo.ScheduleStatus.Next);
        }
    }

    /// <summary>
    /// Retrieves weekly risk data. In the legacy system this called
    /// SqlHelper.ExecuteStoredProcedure(StoredProcedures.GetWeeklyRiskData).
    /// Production deployments should integrate with Azure SQL or Cosmos DB.
    /// </summary>
    private Task<List<RiskRecord>> GetWeeklyRiskDataAsync()
    {
        // Placeholder: production should query the database via usp_GetWeeklyRiskData
        var records = new List<RiskRecord>
        {
            new() { Category = "Market Risk", Exposure = 1250000m, Limit = 2000000m, Utilization = 62.5m },
            new() { Category = "Credit Risk", Exposure = 800000m, Limit = 1500000m, Utilization = 53.3m },
            new() { Category = "Operational Risk", Exposure = 200000m, Limit = 500000m, Utilization = 40.0m }
        };

        return Task.FromResult(records);
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

    private sealed class RiskRecord
    {
        public string Category { get; set; } = string.Empty;
        public decimal Exposure { get; set; }
        public decimal Limit { get; set; }
        public decimal Utilization { get; set; }
    }
}
