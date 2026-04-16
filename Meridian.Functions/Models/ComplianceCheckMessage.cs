namespace Meridian.Functions.Models;

public class ComplianceCheckMessage
{
    public string? DocumentId { get; set; }
    public string? FileName { get; set; }
    public string? DocumentType { get; set; }
    public string? ExtractedData { get; set; }
    public DateTime ExtractedAt { get; set; }
    public int ClientId { get; set; }
}
