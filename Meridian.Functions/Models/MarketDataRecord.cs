namespace Meridian.Functions.Models;

public class MarketDataRecord
{
    public int SecurityId { get; set; }
    public string? Symbol { get; set; }
    public decimal ClosePrice { get; set; }
    public long Volume { get; set; }
    public string? Source { get; set; }
    public DateTime AsOfDate { get; set; }
}
