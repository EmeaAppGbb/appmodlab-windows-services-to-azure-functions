using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Meridian.Functions.Functions.Health;

/// <summary>
/// HTTP-triggered health check endpoint that reports on the overall health
/// of the Function App, Azure Storage connectivity, and SQL connectivity.
/// </summary>
public class HealthCheckFunction
{
    private readonly ILogger<HealthCheckFunction> _logger;

    public HealthCheckFunction(ILogger<HealthCheckFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(CheckHealth))]
    public async Task<IActionResult> CheckHealth(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req,
        FunctionContext context)
    {
        _logger.LogInformation("Health check requested at {Time}", DateTime.UtcNow);

        var healthResult = new HealthCheckResult
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") ?? "local",
            Checks = new List<ComponentHealth>()
        };

        // Check Function App status
        healthResult.Checks.Add(new ComponentHealth
        {
            Component = "FunctionApp",
            Status = "Healthy",
            Description = "Function App is running"
        });

        // Check Azure Storage connectivity
        var storageHealth = await CheckStorageConnectivityAsync();
        healthResult.Checks.Add(storageHealth);

        // Check SQL connectivity
        var sqlHealth = await CheckSqlConnectivityAsync();
        healthResult.Checks.Add(sqlHealth);

        // Overall status is degraded if any component is unhealthy
        if (healthResult.Checks.Any(c => c.Status == "Unhealthy"))
        {
            healthResult.Status = "Unhealthy";
        }
        else if (healthResult.Checks.Any(c => c.Status == "Degraded"))
        {
            healthResult.Status = "Degraded";
        }

        _logger.LogInformation("Health check completed: {Status}", healthResult.Status);

        var statusCode = healthResult.Status == "Healthy" ? 200 : 503;
        return new ObjectResult(healthResult) { StatusCode = statusCode };
    }

    private async Task<ComponentHealth> CheckStorageConnectivityAsync()
    {
        try
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            if (string.IsNullOrEmpty(connectionString))
            {
                return new ComponentHealth
                {
                    Component = "AzureStorage",
                    Status = "Degraded",
                    Description = "Storage connection string not configured"
                };
            }

            var blobServiceClient = new BlobServiceClient(connectionString);
            var properties = await blobServiceClient.GetPropertiesAsync();

            return new ComponentHealth
            {
                Component = "AzureStorage",
                Status = "Healthy",
                Description = "Storage account is accessible"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Storage health check failed");
            return new ComponentHealth
            {
                Component = "AzureStorage",
                Status = "Unhealthy",
                Description = $"Storage connectivity error: {ex.Message}"
            };
        }
    }

    private async Task<ComponentHealth> CheckSqlConnectivityAsync()
    {
        try
        {
            var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
            if (string.IsNullOrEmpty(connectionString))
            {
                return new ComponentHealth
                {
                    Component = "SqlDatabase",
                    Status = "Degraded",
                    Description = "SQL connection string not configured"
                };
            }

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync();

            return new ComponentHealth
            {
                Component = "SqlDatabase",
                Status = "Healthy",
                Description = "SQL database is accessible"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SQL health check failed");
            return new ComponentHealth
            {
                Component = "SqlDatabase",
                Status = "Unhealthy",
                Description = $"SQL connectivity error: {ex.Message}"
            };
        }
    }

    private sealed class HealthCheckResult
    {
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Version { get; set; } = string.Empty;
        public List<ComponentHealth> Checks { get; set; } = new();
    }

    private sealed class ComponentHealth
    {
        public string Component { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
