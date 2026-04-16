using Meridian.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace Meridian.Functions.Functions.Orchestration;

/// <summary>
/// Activity functions for the document processing Durable Functions orchestration.
/// Each activity mirrors a step in the existing queue-driven pipeline but is
/// invoked directly by the orchestrator instead of through storage queues.
/// </summary>
public class DocumentProcessingActivities
{
    private readonly ILogger<DocumentProcessingActivities> _logger;

    public DocumentProcessingActivities(ILogger<DocumentProcessingActivities> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Classifies a document by file name and extension.
    /// Mirrors the logic in <see cref="DocumentProcessing.ClassifyDocumentFunction"/>.
    /// </summary>
    [Function(nameof(ClassifyActivity))]
    public Task<ClassificationResult> ClassifyActivity(
        [ActivityTrigger] DocumentProcessingInput input)
    {
        _logger.LogInformation("Classifying document: {BlobName}", input.BlobName);

        var extension = Path.GetExtension(input.BlobName ?? string.Empty).ToLowerInvariant();
        var fileName = Path.GetFileName(input.BlobName ?? string.Empty).ToLowerInvariant();

        var documentType = extension switch
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

        _logger.LogInformation("Document {BlobName} classified as: {DocumentType}", input.BlobName, documentType);

        var result = new ClassificationResult
        {
            DocumentId = Guid.NewGuid().ToString(),
            FileName = input.BlobName,
            DocumentType = documentType,
            BlobUri = input.BlobUri,
            ContentType = input.ContentType,
            ClientId = 0,
            ReceivedDate = input.UploadedAt
        };

        return Task.FromResult(result);
    }

    /// <summary>
    /// Extracts data from a document. In this orchestrated version, extraction
    /// is simulated — production code would read from blob storage as the
    /// existing <see cref="DocumentProcessing.ExtractDataFunction"/> does.
    /// </summary>
    [Function(nameof(ExtractActivity))]
    public Task<ExtractionResult> ExtractActivity(
        [ActivityTrigger] ClassificationResult input)
    {
        _logger.LogInformation("Extracting data from document: {FileName}, Type: {DocumentType}",
            input.FileName, input.DocumentType);

        // Simulate extraction — in production this would read the blob content.
        var extractedData = $"Extracted content from {input.FileName} " +
                            $"(type: {input.DocumentType}, received: {input.ReceivedDate:O}). " +
                            "Records: 42\nColumns: Date,Symbol,Quantity,Price\n" +
                            "2024-01-15,MSFT,100,400.50";

        _logger.LogInformation("Extracted {Length} characters from {FileName}",
            extractedData.Length, input.FileName);

        var result = new ExtractionResult
        {
            DocumentId = input.DocumentId,
            FileName = input.FileName,
            DocumentType = input.DocumentType,
            ExtractedData = extractedData,
            ClientId = input.ClientId,
            ExtractedAt = DateTime.UtcNow
        };

        return Task.FromResult(result);
    }

    /// <summary>
    /// Evaluates a single compliance rule against extracted document data.
    /// Used in the fan-out/fan-in pattern — one instance per rule executes in parallel.
    /// Mirrors the rule evaluation logic from <see cref="DocumentProcessing.ComplianceCheckFunction"/>.
    /// </summary>
    [Function(nameof(ComplianceCheckActivity))]
    public Task<ComplianceRuleResult> ComplianceCheckActivity(
        [ActivityTrigger] ComplianceRuleInput input)
    {
        _logger.LogInformation("Evaluating compliance rule '{RuleName}' for document {FileName}",
            input.RuleName, input.FileName);

        var data = input.ExtractedData ?? string.Empty;
        var expression = input.RuleExpression ?? string.Empty;
        var passed = EvaluateRule(data, expression);

        _logger.LogInformation("Rule '{RuleName}' result: {Result}", input.RuleName, passed ? "PASS" : "FAIL");

        return Task.FromResult(new ComplianceRuleResult
        {
            RuleName = input.RuleName,
            Expression = input.RuleExpression,
            Passed = passed
        });
    }

    /// <summary>
    /// Sends a notification about the completed document processing pipeline.
    /// Mirrors <see cref="DocumentProcessing.NotifyFunction"/> but logs instead of emailing.
    /// </summary>
    [Function(nameof(NotifyActivity))]
    public Task<bool> NotifyActivity(
        [ActivityTrigger] NotificationMessage input)
    {
        _logger.LogInformation(
            "Sending notification for document: {FileName}, Compliance: {Result}",
            input.FileName, input.ComplianceResult);

        _logger.LogInformation(
            "Notification — Document: {FileName}, Type: {DocumentType}, " +
            "Compliance: {ComplianceResult}, Passed: {RulesPassed}, Failed: {RulesFailed}, " +
            "Processed: {ProcessedAt:O}",
            input.FileName, input.DocumentType,
            input.ComplianceResult, input.RulesPassed, input.RulesFailed,
            input.ProcessedAt);

        return Task.FromResult(true);
    }

    private static bool EvaluateRule(string data, string expression)
    {
        if (string.IsNullOrEmpty(data))
            return false;

        if (expression.Contains("CONTAINS"))
        {
            var keyword = expression.Replace("CONTAINS:", "").Trim();
            return data.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        }

        if (expression.Contains("LENGTH"))
        {
            var minLengthStr = expression.Replace("LENGTH>", "").Trim();
            if (int.TryParse(minLengthStr, out var minLength))
            {
                return data.Length > minLength;
            }
        }

        return true;
    }
}
