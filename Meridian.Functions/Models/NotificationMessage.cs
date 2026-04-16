namespace Meridian.Functions.Models;

public class NotificationMessage
{
    public string? DocumentId { get; set; }
    public string? FileName { get; set; }
    public string? DocumentType { get; set; }
    public string? ComplianceResult { get; set; }
    public int RulesPassed { get; set; }
    public int RulesFailed { get; set; }
    public string? RecipientEmail { get; set; }
    public DateTime ProcessedAt { get; set; }
}
