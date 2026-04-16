namespace Meridian.Functions.Models;

/// <summary>
/// Output of the classify activity — carries the document ID and detected type
/// forward through the orchestration pipeline.
/// </summary>
public class ClassificationResult
{
    public string? DocumentId { get; set; }
    public string? FileName { get; set; }
    public string? DocumentType { get; set; }
    public string? BlobUri { get; set; }
    public string? ContentType { get; set; }
    public int ClientId { get; set; }
    public DateTime ReceivedDate { get; set; }
}
