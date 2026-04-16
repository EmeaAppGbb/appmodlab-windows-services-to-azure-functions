namespace Meridian.Functions.Models;

public class OnDemandReportRequest
{
    public string? ReportType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? OutputFormat { get; set; }
}
