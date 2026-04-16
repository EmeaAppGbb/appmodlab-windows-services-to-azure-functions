namespace Meridian.Functions.Models;

/// <summary>
/// Final output of the document processing orchestration.
/// </summary>
public class DocumentProcessingResult
{
    public string? DocumentId { get; set; }
    public string? FileName { get; set; }
    public string? DocumentType { get; set; }
    public string? ComplianceResult { get; set; }
    public int RulesPassed { get; set; }
    public int RulesFailed { get; set; }
    public bool NotificationSent { get; set; }
    public DateTime CompletedAt { get; set; }
}
