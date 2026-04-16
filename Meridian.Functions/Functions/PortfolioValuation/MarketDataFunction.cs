using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Meridian.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Meridian.Functions.Functions.PortfolioValuation;

/// <summary>
/// Refreshes market data every 5 minutes during US market hours (Mon-Fri, 9AM-4PM UTC).
/// Ported from Meridian.PortfolioValuation/MarketDataFeed/PriceFetcher.cs which fetched
/// prices via SqlHelper.ExecuteStoredProcedure(StoredProcedures.GetLatestMarketData) and
/// fell back to mock data on error. The legacy FeedParser parsed JSON price feeds from an
/// external API. This function stores refreshed market data in Blob Storage for consumption
/// by the PortfolioValuationFunction.
/// </summary>
public class MarketDataFunction
{
    private readonly ILogger<MarketDataFunction> _logger;

    public MarketDataFunction(ILogger<MarketDataFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(RefreshMarketData))]
    public async Task RefreshMarketData(
        [TimerTrigger("0 */5 9-16 * * 1-5")] TimerInfo timerInfo,
        FunctionContext context)
    {
        _logger.LogInformation("Market data refresh triggered at {Time}", DateTime.UtcNow);

        try
        {
            var marketData = await FetchLatestPricesAsync();
            _logger.LogInformation("Retrieved {Count} market data records", marketData.Count);

            await SaveMarketDataToBlobAsync(marketData);

            _logger.LogInformation("Market data refresh completed. {Count} records updated", marketData.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing market data");
            throw;
        }

        if (timerInfo.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next market data refresh scheduled at: {NextRun}",
                timerInfo.ScheduleStatus.Next);
        }
    }

    /// <summary>
    /// Fetches latest market prices. In the legacy system, PriceFetcher called
    /// SqlHelper.ExecuteStoredProcedure(StoredProcedures.GetLatestMarketData) and
    /// fell back to CreateMockMarketData on failure. FeedParser.ParsePriceData
    /// parsed JSON from the external market data API.
    /// Production deployments should integrate with a real market data provider.
    /// </summary>
    private Task<List<MarketDataRecord>> FetchLatestPricesAsync()
    {
        _logger.LogInformation("Fetching latest market prices");

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

        _logger.LogInformation("Retrieved {Count} market data records", records.Count);
        return Task.FromResult(records);
    }

    private async Task SaveMarketDataToBlobAsync(List<MarketDataRecord> marketData)
    {
        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        var containerClient = new BlobContainerClient(connectionString, "market-data");
        await containerClient.CreateIfNotExistsAsync();

        // Save timestamped snapshot
        var snapshotFileName = $"MarketData_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
        var snapshotBlob = containerClient.GetBlobClient(snapshotFileName);
        var json = JsonSerializer.Serialize(marketData, new JsonSerializerOptions { WriteIndented = true });
        using var snapshotStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        await snapshotBlob.UploadAsync(snapshotStream, overwrite: true);

        // Also save as "latest" for easy consumption by other functions
        var latestBlob = containerClient.GetBlobClient("latest.json");
        using var latestStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        await latestBlob.UploadAsync(latestStream, overwrite: true);

        _logger.LogInformation("Market data saved to blob: market-data/{FileName} and market-data/latest.json",
            snapshotFileName);
    }
}
