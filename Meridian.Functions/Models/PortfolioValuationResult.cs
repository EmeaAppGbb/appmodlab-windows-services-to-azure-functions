namespace Meridian.Functions.Models;

public class PortfolioValuationResult
{
    public int PortfolioId { get; set; }
    public string? PortfolioName { get; set; }
    public decimal NAV { get; set; }
    public decimal DailyReturn { get; set; }
    public decimal MTDReturn { get; set; }
    public decimal YTDReturn { get; set; }
    public DateTime AsOfDate { get; set; }
}
