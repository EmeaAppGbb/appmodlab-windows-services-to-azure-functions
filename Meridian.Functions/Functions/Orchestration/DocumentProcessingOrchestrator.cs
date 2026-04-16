using Meridian.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace Meridian.Functions.Functions.Orchestration;

/// <summary>
/// Durable Functions orchestrator for the document processing pipeline.
/// Orchestrates: classify → extract → compliance checks (fan-out/fan-in) → notify.
/// Replaces the queue-chained pipeline with a single, observable orchestration.
/// </summary>
public class DocumentProcessingOrchestrator
{
    /// <summary>
    /// The main orchestrator function. Orchestrates the full document processing
    /// pipeline with retry policies and fan-out/fan-in for compliance rules.
    /// </summary>
    [Function(nameof(RunDocumentProcessing))]
    public async Task<DocumentProcessingResult> RunDocumentProcessing(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var logger = context.CreateReplaySafeLogger<DocumentProcessingOrchestrator>();
        var input = context.GetInput<DocumentProcessingInput>()
            ?? throw new ArgumentNullException(nameof(context), "Orchestration input is required.");

        var retryOptions = new TaskOptions(
            new TaskRetryOptions(new RetryPolicy(
                maxNumberOfAttempts: 3,
                firstRetryInterval: TimeSpan.FromSeconds(5))));

        // Step 1: Classify the document
        logger.LogInformation("Step 1 — Classifying document: {BlobName}", input.BlobName);
        var classification = await context.CallActivityAsync<ClassificationResult>(
            nameof(DocumentProcessingActivities.ClassifyActivity), input, retryOptions);

        // Step 2: Extract data from the document
        logger.LogInformation("Step 2 — Extracting data from: {FileName}", classification.FileName);
        var extraction = await context.CallActivityAsync<ExtractionResult>(
            nameof(DocumentProcessingActivities.ExtractActivity), classification, retryOptions);

        // Step 3: Fan-out — run compliance rules in parallel
        logger.LogInformation("Step 3 — Running compliance checks for: {FileName}", extraction.FileName);

        var rules = GetComplianceRules(extraction.DocumentType ?? string.Empty);
        var complianceTasks = new List<Task<ComplianceRuleResult>>();

        foreach (var rule in rules)
        {
            var ruleInput = new ComplianceRuleInput
            {
                DocumentId = extraction.DocumentId,
                FileName = extraction.FileName,
                DocumentType = extraction.DocumentType,
                ExtractedData = extraction.ExtractedData,
                RuleName = rule.RuleName,
                RuleExpression = rule.Expression
            };

            complianceTasks.Add(context.CallActivityAsync<ComplianceRuleResult>(
                nameof(DocumentProcessingActivities.ComplianceCheckActivity), ruleInput, retryOptions));
        }

        // Fan-in — wait for all compliance checks to complete
        var ruleResults = await Task.WhenAll(complianceTasks);

        var passedCount = ruleResults.Count(r => r.Passed);
        var failedCount = ruleResults.Count(r => !r.Passed);
        var overallResult = failedCount == 0 ? "PASS" : "FAIL";

        logger.LogInformation(
            "Compliance complete — {Passed} passed, {Failed} failed, Overall: {Result}",
            passedCount, failedCount, overallResult);

        // Step 4: Send notification
        logger.LogInformation("Step 4 — Sending notification for: {FileName}", extraction.FileName);
        var notification = new NotificationMessage
        {
            DocumentId = extraction.DocumentId,
            FileName = extraction.FileName,
            DocumentType = extraction.DocumentType,
            ComplianceResult = overallResult,
            RulesPassed = passedCount,
            RulesFailed = failedCount,
            RecipientEmail = "compliance@meridiancapital.com",
            ProcessedAt = context.CurrentUtcDateTime
        };

        var notificationSent = await context.CallActivityAsync<bool>(
            nameof(DocumentProcessingActivities.NotifyActivity), notification, retryOptions);

        return new DocumentProcessingResult
        {
            DocumentId = extraction.DocumentId,
            FileName = extraction.FileName,
            DocumentType = extraction.DocumentType,
            ComplianceResult = overallResult,
            RulesPassed = passedCount,
            RulesFailed = failedCount,
            NotificationSent = notificationSent,
            CompletedAt = context.CurrentUtcDateTime
        };
    }

    /// <summary>
    /// Returns the set of compliance rules to evaluate, matching the logic
    /// in <see cref="DocumentProcessing.ComplianceCheckFunction"/>.
    /// </summary>
    private static List<ComplianceRuleResult> GetComplianceRules(string documentType)
    {
        var rules = new List<ComplianceRuleResult>
        {
            new() { RuleName = "DataPresent", Expression = "LENGTH>0" },
            new() { RuleName = "MinimumContent", Expression = "LENGTH>10" }
        };

        switch (documentType)
        {
            case "AccountStatement":
            case "TradeConfirmation":
                rules.Add(new ComplianceRuleResult { RuleName = "HasRecordData", Expression = "LENGTH>50" });
                break;
            case "PositionFile":
            case "TransactionFile":
                rules.Add(new ComplianceRuleResult { RuleName = "HasRecords", Expression = "CONTAINS:Records" });
                break;
            case "ComplianceReport":
                rules.Add(new ComplianceRuleResult { RuleName = "HasReportContent", Expression = "LENGTH>100" });
                break;
        }

        return rules;
    }
}
