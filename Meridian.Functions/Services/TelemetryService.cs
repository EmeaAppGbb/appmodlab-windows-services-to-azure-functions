using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;

namespace Meridian.Functions.Services;

/// <summary>
/// Wrapper service for tracking custom metrics and events via Application Insights.
/// Centralises telemetry for document processing, compliance checks, portfolio
/// valuations, and alert handling so dashboards and alerts can be built on
/// well-known metric names.
/// </summary>
public interface ITelemetryService
{
    /// <summary>Records how long a document took to process end-to-end.</summary>
    void TrackDocumentProcessingDuration(string fileName, string documentType, TimeSpan duration);

    /// <summary>Records the outcome of a compliance check (pass or fail).</summary>
    void TrackComplianceCheckResult(string fileName, bool passed, int rulesPassed, int rulesFailed);

    /// <summary>Records how long a portfolio valuation cycle took.</summary>
    void TrackPortfolioValuationDuration(string portfolioName, TimeSpan duration, decimal nav);

    /// <summary>Increments the alert counter for a given alert type.</summary>
    void TrackAlert(string alertType, string portfolioName);

    /// <summary>Records a generic custom metric value.</summary>
    void TrackMetric(string metricName, double value, IDictionary<string, string>? properties = null);

    /// <summary>Records a custom event with optional properties.</summary>
    void TrackEvent(string eventName, IDictionary<string, string>? properties = null);
}

public class TelemetryService : ITelemetryService
{
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<TelemetryService> _logger;

    public TelemetryService(TelemetryClient telemetryClient, ILogger<TelemetryService> logger)
    {
        _telemetryClient = telemetryClient;
        _logger = logger;
    }

    public void TrackDocumentProcessingDuration(string fileName, string documentType, TimeSpan duration)
    {
        var properties = new Dictionary<string, string>
        {
            ["FileName"] = fileName,
            ["DocumentType"] = documentType
        };

        _telemetryClient.TrackMetric("DocumentProcessingDurationMs", duration.TotalMilliseconds, properties);
        _telemetryClient.TrackEvent("DocumentProcessed", properties);

        _logger.LogDebug("Tracked document processing: {FileName}, Type={DocumentType}, Duration={DurationMs}ms",
            fileName, documentType, duration.TotalMilliseconds);
    }

    public void TrackComplianceCheckResult(string fileName, bool passed, int rulesPassed, int rulesFailed)
    {
        var properties = new Dictionary<string, string>
        {
            ["FileName"] = fileName,
            ["Result"] = passed ? "Pass" : "Fail",
            ["RulesPassed"] = rulesPassed.ToString(),
            ["RulesFailed"] = rulesFailed.ToString()
        };

        _telemetryClient.TrackEvent("ComplianceCheckCompleted", properties);
        _telemetryClient.TrackMetric("ComplianceCheckPassRate",
            rulesPassed + rulesFailed > 0 ? (double)rulesPassed / (rulesPassed + rulesFailed) * 100 : 0,
            properties);

        if (!passed)
        {
            _telemetryClient.TrackMetric("ComplianceCheckFailures", 1, properties);
        }

        _logger.LogDebug("Tracked compliance check: {FileName}, Passed={Passed}, Rules={RulesPassed}/{Total}",
            fileName, passed, rulesPassed, rulesPassed + rulesFailed);
    }

    public void TrackPortfolioValuationDuration(string portfolioName, TimeSpan duration, decimal nav)
    {
        var properties = new Dictionary<string, string>
        {
            ["PortfolioName"] = portfolioName,
            ["NAV"] = nav.ToString("F2")
        };

        _telemetryClient.TrackMetric("PortfolioValuationDurationMs", duration.TotalMilliseconds, properties);
        _telemetryClient.TrackEvent("PortfolioValuationCompleted", properties);

        _logger.LogDebug("Tracked portfolio valuation: {PortfolioName}, Duration={DurationMs}ms, NAV={NAV:C}",
            portfolioName, duration.TotalMilliseconds, nav);
    }

    public void TrackAlert(string alertType, string portfolioName)
    {
        var properties = new Dictionary<string, string>
        {
            ["AlertType"] = alertType,
            ["PortfolioName"] = portfolioName
        };

        _telemetryClient.TrackMetric("AlertCount", 1, properties);
        _telemetryClient.TrackEvent("AlertRaised", properties);

        _logger.LogDebug("Tracked alert: Type={AlertType}, Portfolio={PortfolioName}", alertType, portfolioName);
    }

    public void TrackMetric(string metricName, double value, IDictionary<string, string>? properties = null)
    {
        _telemetryClient.TrackMetric(metricName, value, properties);
    }

    public void TrackEvent(string eventName, IDictionary<string, string>? properties = null)
    {
        _telemetryClient.TrackEvent(eventName, properties);
    }
}
