using System.Diagnostics;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Meridian.Functions.Models;
using Meridian.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Meridian.Functions.Functions.DocumentProcessing;

public class ProcessDocumentFunction
{
    private readonly ILogger<ProcessDocumentFunction> _logger;
    private readonly ITelemetryService _telemetry;

    public ProcessDocumentFunction(ILogger<ProcessDocumentFunction> logger, ITelemetryService telemetry)
    {
        _logger = logger;
        _telemetry = telemetry;
    }

    [Function(nameof(ProcessDocument))]
    public async Task ProcessDocument(
        [BlobTrigger("incoming-documents/{name}", Connection = "AzureWebJobsStorage")] Stream blobStream,
        string name,
        FunctionContext context)
    {
        _logger.LogInformation("New document detected: {FileName}, Size: {Size} bytes", name, blobStream.Length);
        var sw = Stopwatch.StartNew();

        var message = new ClassifyDocumentMessage
        {
            BlobName = name,
            BlobUri = $"incoming-documents/{name}",
            ContentType = Path.GetExtension(name).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".csv" => "text/csv",
                ".xml" => "application/xml",
                _ => "application/octet-stream"
            },
            ContentLength = blobStream.Length,
            UploadedAt = DateTime.UtcNow
        };

        // Send message to classification queue
        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        var queueClient = new QueueClient(connectionString, "document-classification");
        await queueClient.CreateIfNotExistsAsync();

        var messageJson = JsonSerializer.Serialize(message);
        await queueClient.SendMessageAsync(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(messageJson)));

        _logger.LogInformation("Document {FileName} queued for classification", name);

        sw.Stop();
        _telemetry.TrackDocumentProcessingDuration(name, message.ContentType ?? "unknown", sw.Elapsed);
    }
}
