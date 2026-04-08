using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Net.Http;
using log4net;
using Meridian.Shared.Data;

namespace Meridian.PortfolioValuation.MarketDataFeed
{
    public class PriceFetcher
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(PriceFetcher));
        private readonly string marketDataApiUrl;
        private readonly FeedParser feedParser;

        public PriceFetcher()
        {
            marketDataApiUrl = ConfigurationManager.AppSettings["MarketDataApiUrl"] ?? "https://api.marketdata.example.com/prices";
            feedParser = new FeedParser();
        }

        public DataTable FetchLatestPrices()
        {
            try
            {
                log.Info("Fetching latest market prices from database");
                var marketData = SqlHelper.ExecuteStoredProcedure(StoredProcedures.GetLatestMarketData);
                log.Info($"Retrieved {marketData.Rows.Count} market data records");
                return marketData;
            }
            catch (Exception ex)
            {
                log.Error("Error fetching market prices", ex);
                return CreateMockMarketData();
            }
        }

        private DataTable CreateMockMarketData()
        {
            log.Info("Creating mock market data");
            var dt = new DataTable();
            dt.Columns.Add("SecurityId", typeof(int));
            dt.Columns.Add("Date", typeof(DateTime));
            dt.Columns.Add("ClosePrice", typeof(decimal));
            dt.Columns.Add("Volume", typeof(long));
            dt.Columns.Add("Source", typeof(string));

            var random = new Random();
            for (int i = 1; i <= 10; i++)
            {
                dt.Rows.Add(i, DateTime.Today, 100m + random.Next(-10, 10), random.Next(100000, 1000000), "MOCK");
            }

            return dt;
        }
    }
}
