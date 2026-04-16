namespace Meridian.Functions.Models;

/// <summary>
/// Input payload for starting a document processing orchestration.
/// </summary>
public class DocumentProcessingInput
{
    public string? BlobName { get; set; }
    public string? BlobUri { get; set; }
    public string? ContentType { get; set; }
    public long ContentLength { get; set; }
    public DateTime UploadedAt { get; set; }
}
