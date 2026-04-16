namespace Meridian.Functions.Models;

public class DocumentMetadata
{
    public string? DocumentId { get; set; }
    public string? FileName { get; set; }
    public string? DocumentType { get; set; }
    public DateTime ReceivedDate { get; set; }
    public DateTime? ProcessedDate { get; set; }
    public string? Status { get; set; }
    public int ClientId { get; set; }
    public string? ExtractedData { get; set; }
}
