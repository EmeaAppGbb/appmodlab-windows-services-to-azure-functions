namespace Meridian.Functions.Models;

public class ExtractionMessage
{
    public string? DocumentId { get; set; }
    public string? BlobName { get; set; }
    public string? BlobUri { get; set; }
    public string? DocumentType { get; set; }
    public string? ContentType { get; set; }
    public DateTime ReceivedDate { get; set; }
    public int ClientId { get; set; }
}
