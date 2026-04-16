namespace Meridian.Functions.Models;

public class ClassifyDocumentMessage
{
    public string? BlobName { get; set; }
    public string? BlobUri { get; set; }
    public string? ContentType { get; set; }
    public long ContentLength { get; set; }
    public DateTime UploadedAt { get; set; }
}
