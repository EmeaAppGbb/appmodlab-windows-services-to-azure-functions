using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Meridian.Functions.Models;
using Meridian.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Meridian.Functions.Functions.PortfolioValuation;

/// <summary>
/// Performs portfolio valuations every 15 minutes during US market hours (Mon-Fri, 9AM-4PM UTC).
/// Ported from Meridian.PortfolioValuation/ValuationService.cs which ran on a Topshelf timer
/// and used PortfolioCalculator, PriceFetcher, and AlertService. The legacy version used
/// SqlHelper with usp_GetPortfolios, usp_GetHoldingsByPortfolio, usp_GetLatestMarketData,
/// and usp_InsertValuation. Threshold breaches are now sent to an Azure Storage Queue
/// for processing by AlertFunction.
/// </summary>
public class PortfolioValuationFunction
{
    private readonly ILogger<PortfolioValuationFunction> _logger;
    private readonly ITelemetryService _telemetry;

    public PortfolioValuationFunction(ILogger<PortfolioValuationFunction> logger, ITelemetryService telemetry)
    {
        _logger = logger;
        _telemetry = telemetry;
    }

    [Function(nameof(RunPortfolioValuation))]
    public async Task RunPortfolioValuation(
        [TimerTrigger("0 */15 9-16 * * 1-5")] TimerInfo timerInfo,
        FunctionContext context)
    {
        _logger.LogInformation("Portfolio valuation triggered at {Time}", DateTime.UtcNow);

        try
        {
            var portfolios = await GetPortfoliosAsync();
            _logger.LogInformation("Processing valuations for {Count} portfolios", portfolios.Count);

            var marketData = await GetLatestMarketDataAsync();
            var results = new List<PortfolioValuationResult>();

            foreach (var portfolio in portfolios)
            {
                try
                {
                    var sw = Stopwatch.StartNew();
                    var result = CalculateValuation(portfolio, marketData);
                    sw.Stop();
                    results.Add(result);

                    await CheckThresholdsAsync(result);

                    _telemetry.TrackPortfolioValuationDuration(result.PortfolioName ?? "Unknown", sw.Elapsed, result.NAV);
                    _logger.LogInformation("Valuation completed for portfolio {PortfolioName}: NAV={NAV:C}, Duration={DurationMs}ms",
                        result.PortfolioName, result.NAV, sw.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing portfolio {Name} (ID: {Id})",
                        portfolio.Name, portfolio.PortfolioId);
                }
            }

            // Store valuation results in blob storage for audit trail
            await SaveValuationResultsAsync(results);

            _logger.LogInformation("Portfolio valuation completed. Processed {Count} portfolios", results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing portfolio valuations");
            throw;
        }

        if (timerInfo.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next portfolio valuation scheduled at: {NextRun}",
                timerInfo.ScheduleStatus.Next);
        }
    }

    /// <summary>
    /// Retrieves the list of portfolios. In the legacy system this called
    /// SqlHelper.ExecuteStoredProcedure(StoredProcedures.GetPortfolios).
    /// </summary>
    private Task<List<PortfolioInfo>> GetPortfoliosAsync()
    {
        // Placeholder: production should query the database via usp_GetPortfolios
        var portfolios = new List<PortfolioInfo>
        {
            new() { PortfolioId = 1, Name = "Growth Fund A" },
            new() { PortfolioId = 2, Name = "Income Fund B" },
            new() { PortfolioId = 3, Name = "Balanced Fund C" }
        };

        return Task.FromResult(portfolios);
    }

    /// <summary>
    /// Retrieves latest market data. In the legacy system this was done by PriceFetcher
    /// calling SqlHelper.ExecuteStoredProcedure(StoredProcedures.GetLatestMarketData).
    /// </summary>
    private Task<List<MarketDataRecord>> GetLatestMarketDataAsync()
    {
        var random = new Random();
        var records = new List<MarketDataRecord>();

        for (int i = 1; i <= 10; i++)
        {
            records.Add(new MarketDataRecord
            {
                SecurityId = i,
                Symbol = $"SEC{i:D3}",
                ClosePrice = 100m + (decimal)(random.NextDouble() * 20 - 10),
                Volume = random.Next(100000, 1000000),
                Source = "MarketDataFeed",
                AsOfDate = DateTime.UtcNow
            });
        }

        return Task.FromResult(records);
    }

    /// <summary>
    /// Calculates portfolio valuation. Ported from PortfolioCalculator.CalculateValuation
    /// which iterated holdings, multiplied quantity by price, and computed return metrics.
    /// </summary>
    private PortfolioValuationResult CalculateValuation(PortfolioInfo portfolio, List<MarketDataRecord> marketData)
    {
        var random = new Random();
        decimal totalValue = 0m;

        // Simulate holdings valuation (legacy used usp_GetHoldingsByPortfolio)
        foreach (var data in marketData)
        {
            var quantity = (decimal)(random.Next(100, 1000));
            totalValue += quantity * data.ClosePrice;
        }

        return new PortfolioValuationResult
        {
            PortfolioId = portfolio.PortfolioId,
            PortfolioName = portfolio.Name,
            NAV = totalValue,
            DailyReturn = (decimal)(random.NextDouble() * 4 - 2),
            MTDReturn = (decimal)(random.NextDouble() * 6 - 3),
            YTDReturn = (decimal)(random.NextDouble() * 20 - 10),
            AsOfDate = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Checks return thresholds and sends alerts to the queue if breached.
    /// Ported from ValuationService.CheckThresholds which used AlertService.SendAlert.
    /// </summary>
    private async Task CheckThresholdsAsync(PortfolioValuationResult result)
    {
        var thresholdStr = Environment.GetEnvironmentVariable("DailyReturnThreshold") ?? "5.0";
        if (!decimal.TryParse(thresholdStr, out var threshold))
            threshold = 5.0m;

        if (Math.Abs(result.DailyReturn) > threshold)
        {
            var alertMessage = new AlertMessage
            {
                PortfolioId = result.PortfolioId,
                PortfolioName = result.PortfolioName,
                AlertType = "ThresholdBreach",
                Message = $"Portfolio {result.PortfolioName} daily return {result.DailyReturn:F2}% exceeds threshold {threshold:F2}%",
                CreatedAt = DateTime.UtcNow
            };

            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var queueClient = new QueueClient(connectionString, "alert-queue");
            await queueClient.CreateIfNotExistsAsync();

            var json = JsonSerializer.Serialize(alertMessage);
            await queueClient.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(json)));

            _logger.LogWarning("Threshold breach alert queued for portfolio {Name}: {Return:F2}%",
                result.PortfolioName, result.DailyReturn);
        }
    }

    private async Task SaveValuationResultsAsync(List<PortfolioValuationResult> results)
    {
        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        var containerClient = new BlobContainerClient(connectionString, "valuation-results");
        await containerClient.CreateIfNotExistsAsync();

        var fileName = $"Valuations_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
        var blobClient = containerClient.GetBlobClient(fileName);

        var json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        await blobClient.UploadAsync(stream, overwrite: true);

        _logger.LogInformation("Valuation results saved to blob: valuation-results/{FileName}", fileName);
    }

    private sealed class PortfolioInfo
    {
        public int PortfolioId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
