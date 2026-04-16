namespace Meridian.Functions.Models;

public class ComplianceReportResult
{
    public string? ReportType { get; set; }
    public string? FileName { get; set; }
    public string? BlobUri { get; set; }
    public DateTime GeneratedAt { get; set; }
    public int TotalRecords { get; set; }
    public int PassedCount { get; set; }
    public int FailedCount { get; set; }
}
