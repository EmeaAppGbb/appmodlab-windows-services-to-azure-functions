namespace Meridian.Functions.Models;

/// <summary>
/// Output of the extract activity — carries extracted text data forward
/// for compliance checking.
/// </summary>
public class ExtractionResult
{
    public string? DocumentId { get; set; }
    public string? FileName { get; set; }
    public string? DocumentType { get; set; }
    public string? ExtractedData { get; set; }
    public int ClientId { get; set; }
    public DateTime ExtractedAt { get; set; }
}
