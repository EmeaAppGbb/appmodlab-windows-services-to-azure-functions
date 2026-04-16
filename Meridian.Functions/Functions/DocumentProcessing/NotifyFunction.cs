using System.Text.Json;
using Azure.Communication.Email;
using Meridian.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Meridian.Functions.Functions.DocumentProcessing;

/// <summary>
/// Sends email notifications for processed documents using Azure Communication Services.
/// Replaces the legacy Meridian.DocumentProcessor EmailNotifier which used System.Net.Mail
/// with SMTP (Office 365). The ACS pattern provides a managed, scalable email service
/// with built-in deliverability and tracking.
/// </summary>
public class NotifyFunction
{
    private readonly ILogger<NotifyFunction> _logger;

    public NotifyFunction(ILogger<NotifyFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(SendNotification))]
    public async Task SendNotification(
        [QueueTrigger("notification-queue", Connection = "AzureWebJobsStorage")] string messageText,
        FunctionContext context)
    {
        var message = JsonSerializer.Deserialize<NotificationMessage>(messageText);
        if (message is null)
        {
            _logger.LogWarning("Received null or invalid notification message");
            return;
        }

        _logger.LogInformation("Sending notification for document: {FileName}, Compliance: {Result}",
            message.FileName, message.ComplianceResult);

        var subject = $"Document Processed: {message.FileName}";
        var body = BuildEmailBody(message);

        var acsConnectionString = Environment.GetEnvironmentVariable("AzureCommunicationServicesConnectionString");

        if (string.IsNullOrEmpty(acsConnectionString))
        {
            // Graceful fallback: log the notification when ACS is not configured.
            // This mirrors the legacy EmailNotifier which logged "(simulated)" notifications.
            _logger.LogWarning("ACS connection string not configured — logging notification instead of sending email");
            _logger.LogInformation("Email Subject: {Subject}", subject);
            _logger.LogInformation("Email Body: {Body}", body);
            _logger.LogInformation("Notification logged for document {FileName}", message.FileName);
            return;
        }

        try
        {
            var emailClient = new EmailClient(acsConnectionString);

            var senderAddress = Environment.GetEnvironmentVariable("AzureCommunicationServicesSenderAddress")
                ?? "DoNotReply@meridiancapital.com";
            var recipientAddress = message.RecipientEmail ?? "compliance@meridiancapital.com";

            var emailMessage = new EmailMessage(
                senderAddress: senderAddress,
                recipientAddress: recipientAddress,
                content: new EmailContent(subject)
                {
                    PlainText = body,
                    Html = BuildHtmlEmailBody(message)
                });

            var operation = await emailClient.SendAsync(Azure.WaitUntil.Started, emailMessage);

            _logger.LogInformation(
                "Email notification sent for {FileName}. Operation ID: {OperationId}",
                message.FileName, operation.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email notification for {FileName}", message.FileName);
            throw;
        }
    }

    /// <summary>
    /// Builds the plain-text email body. Mirrors the legacy EmailNotifier.SendProcessingNotification format.
    /// </summary>
    private static string BuildEmailBody(NotificationMessage message)
    {
        return $"""
            Document Processing Complete

            File Name: {message.FileName}
            Document Type: {message.DocumentType}
            Compliance Result: {message.ComplianceResult}
            Rules Passed: {message.RulesPassed}
            Rules Failed: {message.RulesFailed}
            Processed Date: {message.ProcessedAt:yyyy-MM-dd HH:mm:ss} UTC

            This is an automated notification from Meridian Capital Document Processor.
            """;
    }

    /// <summary>
    /// Builds an HTML email body for richer client rendering.
    /// </summary>
    private static string BuildHtmlEmailBody(NotificationMessage message)
    {
        var resultColor = message.ComplianceResult == "PASS" ? "#28a745" : "#dc3545";

        return $"""
            <html>
            <body style="font-family: Arial, sans-serif; color: #333;">
                <h2>Document Processing Complete</h2>
                <table style="border-collapse: collapse; width: 100%; max-width: 600px;">
                    <tr><td style="padding: 8px; font-weight: bold;">File Name:</td><td style="padding: 8px;">{message.FileName}</td></tr>
                    <tr><td style="padding: 8px; font-weight: bold;">Document Type:</td><td style="padding: 8px;">{message.DocumentType}</td></tr>
                    <tr><td style="padding: 8px; font-weight: bold;">Compliance Result:</td><td style="padding: 8px; color: {resultColor}; font-weight: bold;">{message.ComplianceResult}</td></tr>
                    <tr><td style="padding: 8px; font-weight: bold;">Rules Passed:</td><td style="padding: 8px;">{message.RulesPassed}</td></tr>
                    <tr><td style="padding: 8px; font-weight: bold;">Rules Failed:</td><td style="padding: 8px;">{message.RulesFailed}</td></tr>
                    <tr><td style="padding: 8px; font-weight: bold;">Processed Date:</td><td style="padding: 8px;">{message.ProcessedAt:yyyy-MM-dd HH:mm:ss} UTC</td></tr>
                </table>
                <p style="margin-top: 20px; font-size: 12px; color: #666;">
                    This is an automated notification from Meridian Capital Document Processor.
                </p>
            </body>
            </html>
            """;
    }
}
