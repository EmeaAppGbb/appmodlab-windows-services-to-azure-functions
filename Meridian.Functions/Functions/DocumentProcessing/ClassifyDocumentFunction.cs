using System.Text.Json;
using Meridian.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Meridian.Functions.Functions.DocumentProcessing;

public class ClassifyDocumentFunction
{
    private readonly ILogger<ClassifyDocumentFunction> _logger;

    public ClassifyDocumentFunction(ILogger<ClassifyDocumentFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(ClassifyDocument))]
    public async Task ClassifyDocument(
        [QueueTrigger("document-classification", Connection = "AzureWebJobsStorage")] string messageText,
        FunctionContext context)
    {
        var message = JsonSerializer.Deserialize<ClassifyDocumentMessage>(messageText);
        if (message is null)
        {
            _logger.LogWarning("Received null or invalid classification message");
            return;
        }

        _logger.LogInformation("Classifying document: {BlobName}", message.BlobName);

        var extension = Path.GetExtension(message.BlobName ?? string.Empty).ToLowerInvariant();
        var fileName = Path.GetFileName(message.BlobName ?? string.Empty).ToLowerInvariant();

        var documentType = ClassifyByNameAndExtension(fileName, extension);

        _logger.LogInformation("Document {BlobName} classified as: {DocumentType}", message.BlobName, documentType);

        var metadata = new DocumentMetadata
        {
            DocumentId = Guid.NewGuid().ToString(),
            FileName = message.BlobName,
            DocumentType = documentType,
            ReceivedDate = message.UploadedAt,
            ProcessedDate = DateTime.UtcNow,
            Status = "Classified"
        };

        _logger.LogInformation("Document metadata created: {DocumentId}, Type: {DocumentType}",
            metadata.DocumentId, metadata.DocumentType);

        await Task.CompletedTask;
    }

    private static string ClassifyByNameAndExtension(string fileName, string extension)
    {
        return extension switch
        {
            ".pdf" when fileName.Contains("statement") || fileName.Contains("stmt") => "AccountStatement",
            ".pdf" when fileName.Contains("trade") || fileName.Contains("confirm") => "TradeConfirmation",
            ".pdf" when fileName.Contains("report") => "ComplianceReport",
            ".pdf" => "GeneralPDF",
            ".csv" when fileName.Contains("position") || fileName.Contains("holdings") => "PositionFile",
            ".csv" when fileName.Contains("transaction") || fileName.Contains("trade") => "TransactionFile",
            ".csv" when fileName.Contains("price") || fileName.Contains("market") => "MarketDataFile",
            ".csv" => "GeneralCSV",
            ".xml" => "XMLData",
            _ => "Unknown"
        };
    }
}
