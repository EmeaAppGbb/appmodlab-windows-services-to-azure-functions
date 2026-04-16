# Meridian.Functions

Azure Functions project (.NET 9, isolated worker model) for Meridian Capital Advisors. This project replaces three legacy Topshelf Windows Services (`Meridian.DocumentProcessor`, `Meridian.ComplianceReporter`, `Meridian.PortfolioValuation`) with serverless Azure Functions.

## Azure Functions

### Document Processing Pipeline

| Name | Trigger Type | Queue / Container | Description |
|------|-------------|-------------------|-------------|
| `ProcessDocument` | Blob | `incoming-documents/{name}` | Detects new documents uploaded to blob storage and queues them for classification. |
| `ClassifyDocument` | Queue | `document-classification` | Classifies documents by file name and extension (e.g., TradeConfirmation, AccountStatement, PositionFile). |
| `ExtractData` | Queue | `extraction-queue` | Extracts text/data from documents (PDF, CSV, XML) and forwards results to the compliance queue. |
| `CheckCompliance` | Queue | `compliance-queue` | Validates extracted data against compliance rules (CONTAINS, LENGTH expressions) and queues notification. |
| `SendNotification` | Queue | `notification-queue` | Sends email notifications via Azure Communication Services (falls back to logging if ACS is not configured). |

### Compliance Reporting

| Name | Trigger Type | Schedule | Description |
|------|-------------|----------|-------------|
| `GenerateDailyComplianceReport` | Timer | `0 0 18 * * *` (daily at 6 PM UTC) | Generates a daily compliance report and uploads it to the `compliance-reports` blob container. |
| `GenerateWeeklyRiskReport` | Timer | `0 0 9 * * 5` (Fridays at 9 AM UTC) | Generates a weekly risk summary (JSON) and uploads it to the `compliance-reports` blob container. |
| `GenerateMonthlyAuditReport` | Timer | `0 0 6 1 * *` (1st of month at 6 AM UTC) | Generates a monthly audit trail report (text + JSON) and uploads to `compliance-reports`. |
| `GenerateOnDemandReport` | HTTP POST | `/api/reports/generate` | REST API for on-demand compliance report generation. Accepts `ReportType` (Daily, Weekly, Monthly) with optional date filters. |

### Portfolio Valuation

| Name | Trigger Type | Schedule / Queue | Description |
|------|-------------|-----------------|-------------|
| `RunPortfolioValuation` | Timer | `0 */15 9-16 * * 1-5` (every 15 min, Mon–Fri 9 AM–4 PM UTC) | Calculates portfolio NAV and return metrics; saves results to `valuation-results` blob container; queues threshold breach alerts. |
| `RefreshMarketData` | Timer | `0 */5 9-16 * * 1-5` (every 5 min, Mon–Fri 9 AM–4 PM UTC) | Fetches latest market prices and stores them in `market-data` blob container (`latest.json` + timestamped snapshots). |
| `ProcessAlert` | Queue | `alert-queue` | Processes portfolio threshold-breach alerts (persists alert, sends email notification). |

### Health

| Name | Trigger Type | Route | Description |
|------|-------------|-------|-------------|
| `CheckHealth` | HTTP GET | `/api/health` | Reports overall health status including Azure Storage and SQL connectivity checks. |

### Durable Functions Orchestration

| Name | Trigger Type | Route / Details | Description |
|------|-------------|----------------|-------------|
| `StartDocumentProcessing` | HTTP POST | `/api/orchestration/document-processing` | Starts a new document processing orchestration instance. |
| `GetDocumentProcessingStatus` | HTTP GET | `/api/orchestration/document-processing/{instanceId}` | Returns the status of an existing orchestration instance. |
| `RunDocumentProcessing` | Orchestration | — | Orchestrates the full pipeline: classify → extract → compliance checks (fan-out/fan-in) → notify. Includes retry policies. |
| `ClassifyActivity` | Activity | — | Classifies a document (orchestration activity). |
| `ExtractActivity` | Activity | — | Extracts data from a document (orchestration activity). |
| `ComplianceCheckActivity` | Activity | — | Evaluates a single compliance rule (runs in parallel via fan-out). |
| `NotifyActivity` | Activity | — | Sends a processing-complete notification (orchestration activity). |

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) (local storage emulator) or an Azure Storage account

## Local Development

1. **Restore and build:**

   ```bash
   dotnet build Meridian.Functions/Meridian.Functions.csproj
   ```

2. **Configure local settings:**

   Edit `Meridian.Functions/local.settings.json` to set connection strings and configuration:

   ```json
   {
     "IsEncrypted": false,
     "Values": {
       "AzureWebJobsStorage": "UseDevelopmentStorage=true",
       "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
       "SqlConnectionString": "<your-sql-connection-string>",
       "DailyReturnThreshold": "5.0",
       "ComplianceNotificationEmail": "compliance@meridiancapital.com",
       "AzureCommunicationServicesConnectionString": "<your-acs-connection-string>",
       "AzureCommunicationServicesSenderAddress": "DoNotReply@meridiancapital.com"
     }
   }
   ```

3. **Start Azurite** (for local blob/queue storage):

   ```bash
   azurite --silent
   ```

4. **Run the Function App:**

   ```bash
   cd Meridian.Functions
   func start
   ```

   The functions will be available at `http://localhost:7071`.

## Deployment

### Azure CLI

```bash
# Create a resource group
az group create --name rg-meridian --location eastus

# Create a storage account
az storage account create --name stmeridian --resource-group rg-meridian --sku Standard_LRS

# Create a Function App (.NET 9, isolated worker)
az functionapp create \
  --name func-meridian \
  --resource-group rg-meridian \
  --storage-account stmeridian \
  --runtime dotnet-isolated \
  --runtime-version 9 \
  --functions-version 4 \
  --os-type Linux

# Deploy
func azure functionapp publish func-meridian
```

### App Settings (required in Azure)

| Setting | Description |
|---------|-------------|
| `AzureWebJobsStorage` | Azure Storage connection string (blob/queue triggers and outputs) |
| `SqlConnectionString` | SQL Server connection string for data access |
| `DailyReturnThreshold` | Portfolio daily return alert threshold (default: `5.0`) |
| `ComplianceNotificationEmail` | Email address for compliance notifications |
| `AzureCommunicationServicesConnectionString` | Azure Communication Services connection string for email |
| `AzureCommunicationServicesSenderAddress` | Sender email address for ACS |

## Blob Containers

| Container | Purpose |
|-----------|---------|
| `incoming-documents` | Trigger source for document processing pipeline |
| `compliance-reports` | Output for daily, weekly, and monthly compliance reports |
| `valuation-results` | Output for portfolio valuation snapshots |
| `market-data` | Market data snapshots and `latest.json` |

## Storage Queues

| Queue | Purpose |
|-------|---------|
| `document-classification` | Document classification messages |
| `extraction-queue` | Data extraction messages |
| `compliance-queue` | Compliance check messages |
| `notification-queue` | Email notification messages |
| `alert-queue` | Portfolio threshold-breach alerts |
