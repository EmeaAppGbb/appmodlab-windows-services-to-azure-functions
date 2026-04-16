namespace Meridian.Functions.Models;

public class AlertMessage
{
    public int PortfolioId { get; set; }
    public string? PortfolioName { get; set; }
    public string? AlertType { get; set; }
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
}
