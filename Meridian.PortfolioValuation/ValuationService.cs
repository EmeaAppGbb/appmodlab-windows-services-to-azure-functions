using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Timers;
using log4net;
using Meridian.PortfolioValuation.MarketDataFeed;
using Meridian.PortfolioValuation.Calculations;
using Meridian.PortfolioValuation.Notifications;
using Meridian.Shared.Data;

namespace Meridian.PortfolioValuation
{
    public class ValuationService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ValuationService));
        private Timer valuationTimer;
        private readonly PriceFetcher priceFetcher;
        private readonly PortfolioCalculator portfolioCalculator;
        private readonly AlertService alertService;

        public ValuationService()
        {
            priceFetcher = new PriceFetcher();
            portfolioCalculator = new PortfolioCalculator();
            alertService = new AlertService();
        }

        public void Start()
        {
            log.Info("Portfolio Valuation Service starting...");

            var intervalMinutes = int.Parse(ConfigurationManager.AppSettings["ValuationIntervalMinutes"] ?? "60");
            valuationTimer = new Timer(intervalMinutes * 60 * 1000);
            valuationTimer.Elapsed += OnValuationTimer;
            valuationTimer.Start();
            log.Info($"Valuation timer started (interval: {intervalMinutes} minutes)");

            log.Info("Portfolio Valuation Service started.");
        }

        public void Stop()
        {
            log.Info("Portfolio Valuation Service stopping...");
            valuationTimer?.Stop();
            log.Info("Portfolio Valuation Service stopped.");
        }

        private void OnValuationTimer(object sender, ElapsedEventArgs e)
        {
            log.Info("Portfolio valuation timer triggered");
            try
            {
                ProcessPortfolioValuations();
            }
            catch (Exception ex)
            {
                log.Error("Error processing portfolio valuations", ex);
            }
        }

        private void ProcessPortfolioValuations()
        {
            var portfolios = SqlHelper.ExecuteStoredProcedure(StoredProcedures.GetPortfolios);
            log.Info($"Processing valuations for {portfolios.Rows.Count} portfolios");

            foreach (DataRow portfolio in portfolios.Rows)
            {
                try
                {
                    var portfolioId = (int)portfolio["PortfolioId"];
                    var portfolioName = portfolio["Name"].ToString();
                    
                    log.Info($"Processing portfolio: {portfolioName} (ID: {portfolioId})");

                    var holdings = SqlHelper.ExecuteStoredProcedure(
                        StoredProcedures.GetHoldingsByPortfolio,
                        new SqlParameter("@PortfolioId", portfolioId));

                    var marketData = priceFetcher.FetchLatestPrices();

                    var valuation = portfolioCalculator.CalculateValuation(portfolioId, holdings, marketData);

                    SaveValuation(valuation);

                    CheckThresholds(portfolioId, portfolioName, valuation);

                    log.Info($"Valuation completed for portfolio {portfolioName}: NAV={valuation.NAV:C}");
                }
                catch (Exception ex)
                {
                    log.Error($"Error processing portfolio {portfolio["Name"]}", ex);
                }
            }
        }

        private void SaveValuation(DataRow valuation)
        {
            var parameters = new[]
            {
                new SqlParameter("@PortfolioId", valuation["PortfolioId"]),
                new SqlParameter("@AsOfDate", DateTime.Now),
                new SqlParameter("@NAV", valuation["NAV"]),
                new SqlParameter("@DailyReturn", valuation["DailyReturn"]),
                new SqlParameter("@MTDReturn", valuation["MTDReturn"]),
                new SqlParameter("@YTDReturn", valuation["YTDReturn"])
            };

            SqlHelper.ExecuteStoredProcedureNonQuery(StoredProcedures.InsertValuation, parameters);
        }

        private void CheckThresholds(int portfolioId, string portfolioName, DataRow valuation)
        {
            var dailyReturn = Convert.ToDecimal(valuation["DailyReturn"]);
            var threshold = decimal.Parse(ConfigurationManager.AppSettings["DailyReturnThreshold"] ?? "5.0");

            if (Math.Abs(dailyReturn) > threshold)
            {
                var message = $"Portfolio {portfolioName} daily return {dailyReturn:F2}% exceeds threshold {threshold:F2}%";
                alertService.SendAlert(portfolioId, "ThresholdBreach", message);
                log.Warn(message);
            }
        }
    }
}
