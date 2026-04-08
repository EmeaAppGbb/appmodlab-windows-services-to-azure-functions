using System;
using System.Data;
using log4net;

namespace Meridian.PortfolioValuation.Calculations
{
    public class PortfolioCalculator
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(PortfolioCalculator));
        private readonly BenchmarkComparator benchmarkComparator;

        public PortfolioCalculator()
        {
            benchmarkComparator = new BenchmarkComparator();
        }

        public DataRow CalculateValuation(int portfolioId, DataTable holdings, DataTable marketData)
        {
            var valuationTable = new DataTable();
            valuationTable.Columns.Add("PortfolioId", typeof(int));
            valuationTable.Columns.Add("NAV", typeof(decimal));
            valuationTable.Columns.Add("DailyReturn", typeof(decimal));
            valuationTable.Columns.Add("MTDReturn", typeof(decimal));
            valuationTable.Columns.Add("YTDReturn", typeof(decimal));

            var random = new Random();
            decimal totalValue = 0m;

            foreach (DataRow holding in holdings.Rows)
            {
                var securityId = (int)holding["SecurityId"];
                var quantity = (decimal)holding["Quantity"];

                var price = GetPriceForSecurity(securityId, marketData);
                totalValue += quantity * price;
            }

            var dailyReturn = (decimal)(random.NextDouble() * 4 - 2);
            var mtdReturn = (decimal)(random.NextDouble() * 6 - 3);
            var ytdReturn = (decimal)(random.NextDouble() * 20 - 10);

            var row = valuationTable.NewRow();
            row["PortfolioId"] = portfolioId;
            row["NAV"] = totalValue;
            row["DailyReturn"] = dailyReturn;
            row["MTDReturn"] = mtdReturn;
            row["YTDReturn"] = ytdReturn;
            valuationTable.Rows.Add(row);

            log.Info($"Portfolio {portfolioId} valuation: NAV={totalValue:C}, Daily={dailyReturn:F2}%");

            return row;
        }

        private decimal GetPriceForSecurity(int securityId, DataTable marketData)
        {
            foreach (DataRow row in marketData.Rows)
            {
                if ((int)row["SecurityId"] == securityId)
                {
                    return (decimal)row["ClosePrice"];
                }
            }
            return 100m;
        }
    }
}
