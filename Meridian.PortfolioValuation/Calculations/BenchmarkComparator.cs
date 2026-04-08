using System;
using log4net;

namespace Meridian.PortfolioValuation.Calculations
{
    public class BenchmarkComparator
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(BenchmarkComparator));

        public decimal CompareAgainstBenchmark(decimal portfolioReturn, string benchmarkName)
        {
            var benchmarkReturn = GetBenchmarkReturn(benchmarkName);
            var tracking = portfolioReturn - benchmarkReturn;
            
            log.Info($"Portfolio return: {portfolioReturn:F2}%, Benchmark ({benchmarkName}): {benchmarkReturn:F2}%, Tracking: {tracking:F2}%");
            
            return tracking;
        }

        private decimal GetBenchmarkReturn(string benchmarkName)
        {
            var random = new Random();
            return (decimal)(random.NextDouble() * 3 - 1.5);
        }
    }
}
