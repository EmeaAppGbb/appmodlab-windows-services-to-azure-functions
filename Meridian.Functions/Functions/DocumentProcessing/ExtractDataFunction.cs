using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Meridian.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Meridian.Functions.Functions.DocumentProcessing;

/// <summary>
/// Extracts financial data from documents, ported from the legacy
/// Meridian.DocumentProcessor DataExtractor / PdfParser / CsvImporter pipeline.
/// Reads the blob content, performs text extraction based on document type,
/// and forwards the result to the compliance-queue.
/// </summary>
public class ExtractDataFunction
{
    private readonly ILogger<ExtractDataFunction> _logger;

    public ExtractDataFunction(ILogger<ExtractDataFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(ExtractData))]
    public async Task ExtractData(
        [QueueTrigger("extraction-queue", Connection = "AzureWebJobsStorage")] string messageText,
        FunctionContext context)
    {
        var message = JsonSerializer.Deserialize<ExtractionMessage>(messageText);
        if (message is null)
        {
            _logger.LogWarning("Received null or invalid extraction message");
            return;
        }

        _logger.LogInformation("Extracting data from document: {BlobName}, Type: {DocumentType}",
            message.BlobName, message.DocumentType);

        string extractedData;
        try
        {
            extractedData = await ExtractFromBlobAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting data from {BlobName}", message.BlobName);
            throw;
        }

        _logger.LogInformation("Extracted {Length} characters from {BlobName}",
            extractedData.Length, message.BlobName);

        // Forward to compliance-queue (mirrors legacy pipeline: extract -> compliance check)
        var complianceMessage = new ComplianceCheckMessage
        {
            DocumentId = message.DocumentId,
            FileName = message.BlobName,
            DocumentType = message.DocumentType,
            ExtractedData = extractedData.Length > 4000
                ? extractedData[..4000]
                : extractedData,
            ExtractedAt = DateTime.UtcNow,
            ClientId = message.ClientId
        };

        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        var queueClient = new QueueClient(connectionString, "compliance-queue");
        await queueClient.CreateIfNotExistsAsync();

        var json = JsonSerializer.Serialize(complianceMessage);
        await queueClient.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(json)));

        _logger.LogInformation("Document {BlobName} queued for compliance check", message.BlobName);
    }

    /// <summary>
    /// Downloads the blob and extracts text based on the content type.
    /// Ported from legacy DataExtractor.ExtractData, PdfParser.ExtractText,
    /// and CsvImporter.ImportCsv.
    /// </summary>
    private async Task<string> ExtractFromBlobAsync(ExtractionMessage message)
    {
        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        var blobServiceClient = new BlobServiceClient(connectionString);

        var containerName = message.BlobUri?.Split('/').FirstOrDefault() ?? "incoming-documents";
        var blobName = message.BlobUri?.Contains('/')  == true
            ? string.Join("/", message.BlobUri.Split('/').Skip(1))
            : message.BlobName ?? string.Empty;

        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        using var memoryStream = new MemoryStream();
        await blobClient.DownloadToAsync(memoryStream);
        memoryStream.Position = 0;

        var extension = Path.GetExtension(message.BlobName ?? string.Empty).ToLowerInvariant();

        return extension switch
        {
            ".pdf" => ExtractTextFromPdfStream(memoryStream),
            ".csv" => ExtractTextFromCsvStream(memoryStream),
            ".xml" => await ExtractTextFromXmlStreamAsync(memoryStream),
            _ => $"Unsupported file type: {extension}"
        };
    }

    /// <summary>
    /// Extracts raw text from a PDF stream.
    /// Ported from legacy PdfParser.ExtractText — reads pages sequentially
    /// and concatenates their text content.
    /// </summary>
    private string ExtractTextFromPdfStream(Stream stream)
    {
        try
        {
            // Read the raw stream bytes and convert to a text representation.
            // The legacy code used iTextSharp (GPL-licensed, .NET Framework only).
            // In the cloud function we work with the raw bytes and extract readable
            // ASCII/UTF-8 text segments — a full PDF library (e.g. PdfPig) can be
            // added later for production-grade extraction.
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var rawContent = reader.ReadToEnd();

            var textBuilder = new StringBuilder();

            // Extract text between PDF stream markers (BT…ET blocks contain text)
            var index = 0;
            while (index < rawContent.Length)
            {
                var btIndex = rawContent.IndexOf("BT", index, StringComparison.Ordinal);
                if (btIndex < 0) break;

                var etIndex = rawContent.IndexOf("ET", btIndex, StringComparison.Ordinal);
                if (etIndex < 0) break;

                var block = rawContent.Substring(btIndex, etIndex - btIndex + 2);
                // Pull text from Tj and TJ operators
                foreach (var line in block.Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (trimmed.EndsWith("Tj") || trimmed.EndsWith("TJ"))
                    {
                        var start = trimmed.IndexOf('(');
                        var end = trimmed.LastIndexOf(')');
                        if (start >= 0 && end > start)
                        {
                            textBuilder.AppendLine(trimmed.Substring(start + 1, end - start - 1));
                        }
                    }
                }

                index = etIndex + 2;
            }

            var text = textBuilder.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                // Fallback: return raw readable characters
                text = new string(rawContent.Where(c => !char.IsControl(c) || c == '\n' || c == '\r').ToArray());
            }

            _logger.LogInformation("Extracted {Length} characters from PDF stream", text.Length);
            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from PDF stream");
            return string.Empty;
        }
    }

    /// <summary>
    /// Reads a CSV stream and produces a summary of its contents.
    /// Ported from legacy CsvImporter.ImportCsv — returns record count and column names.
    /// </summary>
    private string ExtractTextFromCsvStream(Stream stream)
    {
        try
        {
            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            var summary = new StringBuilder();
            summary.AppendLine($"Records: {Math.Max(0, lines.Length - 1)}");

            if (lines.Length > 0)
            {
                summary.AppendLine($"Columns: {lines[0].Trim()}");
            }

            // Include first few data rows for downstream compliance checks
            for (var i = 1; i < Math.Min(lines.Length, 6); i++)
            {
                summary.AppendLine(lines[i].Trim());
            }

            _logger.LogInformation("Imported {RecordCount} records from CSV stream", Math.Max(0, lines.Length - 1));
            return summary.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing CSV stream");
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Reads XML content as plain text for downstream processing.
    /// </summary>
    private static async Task<string> ExtractTextFromXmlStreamAsync(Stream stream)
    {
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
