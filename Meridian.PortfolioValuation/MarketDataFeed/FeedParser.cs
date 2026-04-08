using System;
using System.Collections.Generic;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Meridian.PortfolioValuation.MarketDataFeed
{
    public class FeedParser
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(FeedParser));

        public Dictionary<string, decimal> ParsePriceData(string jsonData)
        {
            var prices = new Dictionary<string, decimal>();

            try
            {
                var data = JObject.Parse(jsonData);
                var quotes = data["quotes"] as JArray;

                if (quotes != null)
                {
                    foreach (var quote in quotes)
                    {
                        var symbol = quote["symbol"]?.ToString();
                        var price = quote["price"]?.ToObject<decimal>() ?? 0m;
                        
                        if (!string.IsNullOrEmpty(symbol))
                        {
                            prices[symbol] = price;
                        }
                    }
                }

                log.Info($"Parsed {prices.Count} price records from feed");
            }
            catch (Exception ex)
            {
                log.Error("Error parsing price feed data", ex);
            }

            return prices;
        }
    }
}
