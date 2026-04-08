using System;

namespace Meridian.Shared.Models
{
    public class Portfolio
    {
        public int PortfolioId { get; set; }
        public int ClientId { get; set; }
        public string Name { get; set; }
        public string Benchmark { get; set; }
        public DateTime InceptionDate { get; set; }
        public string Currency { get; set; }
    }

    public class Holding
    {
        public int HoldingId { get; set; }
        public int PortfolioId { get; set; }
        public int SecurityId { get; set; }
        public decimal Quantity { get; set; }
        public decimal CostBasis { get; set; }
        public DateTime AsOfDate { get; set; }
    }

    public class Valuation
    {
        public int ValuationId { get; set; }
        public int PortfolioId { get; set; }
        public DateTime AsOfDate { get; set; }
        public decimal NAV { get; set; }
        public decimal DailyReturn { get; set; }
        public decimal MTDReturn { get; set; }
        public decimal YTDReturn { get; set; }
    }

    public class MarketData
    {
        public int SecurityId { get; set; }
        public DateTime Date { get; set; }
        public decimal ClosePrice { get; set; }
        public long Volume { get; set; }
        public string Source { get; set; }
    }

    public class Alert
    {
        public int AlertId { get; set; }
        public int PortfolioId { get; set; }
        public string AlertType { get; set; }
        public string Message { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? AcknowledgedDate { get; set; }
    }
}
