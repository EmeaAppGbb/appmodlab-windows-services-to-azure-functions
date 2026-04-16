using Meridian.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService(options =>
        {
            options.EnableAdaptiveSampling = false;
        });
        services.ConfigureFunctionsApplicationInsights();

        // Register custom telemetry service for structured metrics
        services.AddSingleton<ITelemetryService, TelemetryService>();
    })
    .ConfigureLogging(logging =>
    {
        logging.SetMinimumLevel(LogLevel.Information);

        logging.AddFilter("Microsoft", LogLevel.Warning);
        logging.AddFilter("System", LogLevel.Warning);
        logging.AddFilter("Azure.Core", LogLevel.Warning);
        logging.AddFilter("Azure.Storage", LogLevel.Warning);

        // Keep Meridian function logs at Information level
        logging.AddFilter("Meridian.Functions", LogLevel.Information);
    })
    .Build();

host.Run();
