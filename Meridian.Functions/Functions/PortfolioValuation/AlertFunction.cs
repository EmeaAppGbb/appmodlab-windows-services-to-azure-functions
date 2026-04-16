using System.Text.Json;
using Meridian.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Meridian.Functions.Functions.PortfolioValuation;

/// <summary>
/// Processes portfolio alert messages from the alert-queue.
/// Ported from Meridian.PortfolioValuation/Notifications/AlertService.cs which used
/// SqlHelper.ExecuteStoredProcedureNonQuery with usp_InsertAlert to persist alerts and
/// sent simulated email notifications. This function processes queue messages generated
/// by PortfolioValuationFunction when threshold breaches are detected.
/// </summary>
public class AlertFunction
{
    private readonly ILogger<AlertFunction> _logger;

    public AlertFunction(ILogger<AlertFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(ProcessAlert))]
    public async Task ProcessAlert(
        [QueueTrigger("alert-queue", Connection = "AzureWebJobsStorage")] string messageText,
        FunctionContext context)
    {
        var alert = JsonSerializer.Deserialize<AlertMessage>(messageText);
        if (alert is null)
        {
            _logger.LogWarning("Received null or invalid alert message");
            return;
        }

        _logger.LogWarning("ALERT [{AlertType}] Portfolio {PortfolioId} ({PortfolioName}): {Message}",
            alert.AlertType, alert.PortfolioId, alert.PortfolioName, alert.Message);

        try
        {
            // Persist the alert (legacy used usp_InsertAlert via SqlHelper)
            await PersistAlertAsync(alert);

            // Send email notification (legacy AlertService.SendEmailAlert was simulated)
            await SendEmailAlertAsync(alert);

            _logger.LogInformation("Alert processed successfully for portfolio {PortfolioId}", alert.PortfolioId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing alert for portfolio {PortfolioId}", alert.PortfolioId);
            throw;
        }
    }

    /// <summary>
    /// Persists the alert record. In the legacy system this called
    /// SqlHelper.ExecuteStoredProcedureNonQuery(StoredProcedures.InsertAlert, ...) with
    /// parameters @PortfolioId, @AlertType, @Message, @CreatedDate.
    /// Production deployments should integrate with Azure SQL or Cosmos DB.
    /// </summary>
    private Task PersistAlertAsync(AlertMessage alert)
    {
        // Placeholder: production should persist to database via usp_InsertAlert
        _logger.LogInformation(
            "Alert persisted (simulated) - Portfolio: {PortfolioId}, Type: {AlertType}, Created: {CreatedAt}",
            alert.PortfolioId, alert.AlertType, alert.CreatedAt);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Sends an email notification for the alert. The legacy AlertService.SendEmailAlert
    /// was a simulated email sender. Production deployments should use Azure Communication
    /// Services (as demonstrated in NotifyFunction) or another email provider.
    /// </summary>
    private Task SendEmailAlertAsync(AlertMessage alert)
    {
        _logger.LogInformation(
            "Email alert sent (simulated) - Portfolio: {PortfolioId}, Type: {AlertType}, Message: {Message}",
            alert.PortfolioId, alert.AlertType, alert.Message);

        return Task.CompletedTask;
    }
}
