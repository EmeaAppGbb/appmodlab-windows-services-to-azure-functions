using Meridian.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace Meridian.Functions.Functions.Orchestration;

/// <summary>
/// HTTP trigger endpoints for starting and querying document processing orchestrations.
/// </summary>
public class DocumentProcessingHttpStarter
{
    private readonly ILogger<DocumentProcessingHttpStarter> _logger;

    public DocumentProcessingHttpStarter(ILogger<DocumentProcessingHttpStarter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Starts a new document processing orchestration instance.
    /// POST /api/orchestration/document-processing
    /// </summary>
    [Function(nameof(StartDocumentProcessing))]
    public async Task<HttpResponseData> StartDocumentProcessing(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orchestration/document-processing")]
        HttpRequestData req,
        [DurableClient] DurableTaskClient client)
    {
        var input = await req.ReadFromJsonAsync<DocumentProcessingInput>();
        if (input is null || string.IsNullOrWhiteSpace(input.BlobName))
        {
            var badRequest = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("Request body must include at least a BlobName.");
            return badRequest;
        }

        if (input.UploadedAt == default)
            input.UploadedAt = DateTime.UtcNow;

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(DocumentProcessingOrchestrator.RunDocumentProcessing), input);

        _logger.LogInformation("Started orchestration {InstanceId} for {BlobName}", instanceId, input.BlobName);

        return await client.CreateCheckStatusResponseAsync(req, instanceId);
    }

    /// <summary>
    /// Returns the status of an existing orchestration instance.
    /// GET /api/orchestration/document-processing/{instanceId}
    /// </summary>
    [Function(nameof(GetDocumentProcessingStatus))]
    public async Task<HttpResponseData> GetDocumentProcessingStatus(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orchestration/document-processing/{instanceId}")]
        HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        string instanceId)
    {
        var metadata = await client.GetInstanceAsync(instanceId, getInputsAndOutputs: true);

        if (metadata is null)
        {
            var notFound = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
            await notFound.WriteStringAsync($"Orchestration instance '{instanceId}' not found.");
            return notFound;
        }

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            metadata.InstanceId,
            metadata.Name,
            RuntimeStatus = metadata.RuntimeStatus.ToString(),
            metadata.CreatedAt,
            metadata.LastUpdatedAt,
            metadata.SerializedInput,
            metadata.SerializedOutput
        });

        return response;
    }
}
