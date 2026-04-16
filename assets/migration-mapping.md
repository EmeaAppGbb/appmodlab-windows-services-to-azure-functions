# Meridian Capital — Windows Services to Azure Functions Migration Mapping

## Table of Contents

1. [Solution Overview](#1-solution-overview)
2. [Service Component Inventory](#2-service-component-inventory)
3. [Azure Functions Trigger Mapping](#3-azure-functions-trigger-mapping)
4. [Detailed Component Mapping](#4-detailed-component-mapping)
5. [Shared Library Refactoring](#5-shared-library-refactoring)
6. [Infrastructure & Cross-Cutting Mapping](#6-infrastructure--cross-cutting-mapping)
7. [Configuration Migration](#7-configuration-migration)
8. [Database & Stored Procedure Inventory](#8-database--stored-procedure-inventory)
9. [NuGet Package Migration](#9-nuget-package-migration)
10. [Risk & Considerations](#10-risk--considerations)

---

## 1. Solution Overview

| Attribute | Current State | Target State |
|-----------|--------------|--------------|
| **Framework** | .NET Framework 4.8 | .NET 8 (isolated worker) |
| **Hosting** | Windows Services (Topshelf) | Azure Functions v4 |
| **Database** | SQL Server (localhost, Windows Auth) | Azure SQL Database (connection string / Managed Identity) |
| **File Storage** | Local filesystem (`C:\MeridianData\`) | Azure Blob Storage |
| **Email** | SMTP (System.Net.Mail) — simulated | Azure Communication Services Email |
| **Logging** | log4net + EventLog | Application Insights / ILogger |
| **Configuration** | App.config (appSettings) | Azure Functions app settings / Azure App Configuration |
| **Solution** | `MeridianDocProcessor.sln` — 4 projects | Azure Functions project(s) + shared class library |

---

## 2. Service Component Inventory

### 2.1 Meridian.DocumentProcessor

**Purpose:** Watches a local directory for incoming financial documents (PDF, CSV, XML), classifies them, extracts data, runs compliance checks, persists results to SQL Server, and sends email notifications.

| Component | File | Responsibility |
|-----------|------|----------------|
| `Program` | `Program.cs` | Topshelf host bootstrap; service registration, recovery settings |
| `DocumentProcessorService` | `DocumentProcessorService.cs` | Service lifecycle (`Start`/`Stop`); creates watch directory, starts `IncomingDocWatcher` |
| `IncomingDocWatcher` | `FileWatcher/IncomingDocWatcher.cs` | Wraps `System.IO.FileSystemWatcher`; detects new files, orchestrates processing pipeline |
| `PdfParser` | `FileWatcher/PdfParser.cs` | Extracts text from PDF files using iTextSharp |
| `CsvImporter` | `FileWatcher/CsvImporter.cs` | Parses CSV files using CsvHelper; returns record count and column summary |
| `DocumentClassifier` | `Processing/DocumentClassifier.cs` | Classifies documents by extension + filename keywords (AccountStatement, TradeConfirmation, etc.) |
| `DataExtractor` | `Processing/DataExtractor.cs` | Delegates to PdfParser/CsvImporter; saves extracted data via `usp_InsertDocument` |
| `ComplianceChecker` | `Processing/ComplianceChecker.cs` | Loads active rules via `usp_GetActiveComplianceRules`; evaluates CONTAINS/LENGTH expressions |
| `EmailNotifier` | `Notifications/EmailNotifier.cs` | Composes and sends SMTP notification (currently simulated via logging) |

**Trigger mechanism:** `FileSystemWatcher` monitoring `C:\MeridianData\Incoming` for `Created` events on `*.*` files, with a 500 ms debounce delay.

### 2.2 Meridian.ComplianceReporter

**Purpose:** Generates scheduled compliance, risk, and audit reports by querying SQL Server and exporting to PDF/Excel files on a local path.

| Component | File | Responsibility |
|-----------|------|----------------|
| `Program` | `Program.cs` | Topshelf host bootstrap |
| `ComplianceReporterService` | `ComplianceReporterService.cs` | Manages three `System.Timers.Timer` instances (daily, weekly, monthly) |
| `DailyComplianceReport` | `Reports/DailyComplianceReport.cs` | Queries `usp_GetDailyComplianceData`; generates PDF via `PdfExporter` |
| `WeeklyRiskReport` | `Reports/WeeklyRiskReport.cs` | Queries `usp_GetWeeklyRiskData`; generates Excel via `ExcelExporter` |
| `MonthlyAuditReport` | `Reports/MonthlyAuditReport.cs` | Queries `usp_GetMonthlyAuditData`; generates PDF + Excel |
| `PdfExporter` | `Exporters/PdfExporter.cs` | Creates PDF documents using iTextSharp (A4, Courier 10pt) |
| `ExcelExporter` | `Exporters/ExcelExporter.cs` | Creates Excel workbooks using EPPlus; headers, data rows, auto-fit |

**Trigger mechanism:** Three independent `System.Timers.Timer` instances:
- Daily: every 1 440 min (24 h) → `DailyComplianceReport.GenerateReport()`
- Weekly: every 10 080 min (7 d) → `WeeklyRiskReport.GenerateReport()`
- Monthly: every 43 200 min (30 d) → `MonthlyAuditReport.GenerateReport()`

### 2.3 Meridian.PortfolioValuation

**Purpose:** Periodically fetches market data, calculates NAV and returns for each portfolio, persists valuations, and raises alerts when daily return thresholds are breached.

| Component | File | Responsibility |
|-----------|------|----------------|
| `Program` | `Program.cs` | Topshelf host bootstrap |
| `ValuationService` | `ValuationService.cs` | Single timer; orchestrates per-portfolio valuation pipeline; saves valuations and alerts to DB |
| `PriceFetcher` | `MarketDataFeed/PriceFetcher.cs` | Reads latest prices from DB (`usp_GetLatestMarketData`); has mock fallback; API URL configured but unused |
| `FeedParser` | `MarketDataFeed/FeedParser.cs` | Parses JSON market data feed (`Newtonsoft.Json`); currently unused by `PriceFetcher` |
| `PortfolioCalculator` | `Calculations/PortfolioCalculator.cs` | Computes NAV (Σ Quantity × Price); generates mock daily/MTD/YTD returns |
| `BenchmarkComparator` | `Calculations/BenchmarkComparator.cs` | Computes tracking error vs. benchmark; mock benchmark returns; not wired into service |
| `AlertService` | `Notifications/AlertService.cs` | Inserts alerts via `usp_InsertAlert`; has placeholder SMTP email method |

**Trigger mechanism:** Single `System.Timers.Timer` every 60 min (configurable via `ValuationIntervalMinutes`).

### 2.4 Meridian.Shared

**Purpose:** Shared class library providing data access and domain models consumed by all three services.

| Component | File | Responsibility |
|-----------|------|----------------|
| `SqlHelper` | `Data/SqlHelper.cs` | Static ADO.NET utility; ExecuteQuery, ExecuteNonQuery, ExecuteScalar, ExecuteStoredProcedure, ExecuteStoredProcedureNonQuery |
| `StoredProcedures` | `Data/StoredProcedures.cs` | String constants for all 16 stored procedure names |
| `Document` | `Models/Document.cs` | POCO: DocumentId, FileName, DocumentType, ReceivedDate, ProcessedDate, Status, ClientId, ExtractedData |
| `Portfolio` | `Models/Portfolio.cs` | POCOs: Portfolio, Holding, Valuation, MarketData, Alert |
| `ComplianceCheck` | `Models/ComplianceCheck.cs` | POCOs: ComplianceCheck, ComplianceRule |

---

## 3. Azure Functions Trigger Mapping

### Master Mapping Table

| Windows Service | Trigger Mechanism | Azure Functions Trigger | Function Name (Proposed) | CRON / Config |
|----------------|-------------------|------------------------|--------------------------|---------------|
| DocumentProcessor | `FileSystemWatcher` on `C:\MeridianData\Incoming` | **Blob Trigger** | `ProcessIncomingDocument` | Container: `incoming-documents`, path: `incoming/{name}` |
| ComplianceReporter (Daily) | `System.Timers.Timer` — 1 440 min | **Timer Trigger** | `GenerateDailyComplianceReport` | `0 0 6 * * *` (daily at 06:00 UTC) |
| ComplianceReporter (Weekly) | `System.Timers.Timer` — 10 080 min | **Timer Trigger** | `GenerateWeeklyRiskReport` | `0 0 7 * * 1` (Monday at 07:00 UTC) |
| ComplianceReporter (Monthly) | `System.Timers.Timer` — 43 200 min | **Timer Trigger** | `GenerateMonthlyAuditReport` | `0 0 8 1 * *` (1st of month at 08:00 UTC) |
| PortfolioValuation | `System.Timers.Timer` — 60 min | **Timer Trigger** | `RunPortfolioValuation` | `0 0 * * * *` (every hour) |
| PortfolioValuation (Alerts) | In-process after valuation | **Queue Trigger** (optional) | `ProcessValuationAlert` | Queue: `valuation-alerts` |

> **Design note:** The PortfolioValuation alert path can remain in-process within the timer function, or be decoupled via an Azure Storage Queue for independent scaling and retry. A Queue Trigger is recommended for production.

### Trigger Type Rationale

| Pattern | Why This Trigger |
|---------|-----------------|
| **Blob Trigger** for DocumentProcessor | Direct replacement for `FileSystemWatcher`. Files uploaded to Blob Storage fire the function automatically. Supports built-in retry, dead-letter via poison blob handling, and scales with Event Grid subscription for low-latency. |
| **Timer Trigger** for scheduled reports | Direct replacement for `System.Timers.Timer`. CRON expressions provide precise scheduling. Azure Functions runtime guarantees single execution per schedule tick (no duplicate processing). |
| **Timer Trigger** for portfolio valuation | Hourly recurrence maps directly to a CRON schedule. Timer triggers are idempotent by design. |
| **Queue Trigger** for alerts | Decouples alert processing from valuation; provides built-in retry, poison-queue handling, and independent scaling. |

---

## 4. Detailed Component Mapping

### 4.1 DocumentProcessor → `ProcessIncomingDocument` (Blob Trigger)

```
┌─────────────────────────────────────────────────────────────────┐
│ CURRENT (Windows Service)            → TARGET (Azure Function)  │
├─────────────────────────────────────────────────────────────────┤
│ FileSystemWatcher.Created event      → Blob Trigger on          │
│   on C:\MeridianData\Incoming           incoming/{name}         │
│                                                                 │
│ Thread.Sleep(500) debounce           → Not needed (blob upload  │
│                                         is atomic)              │
│                                                                 │
│ DocumentClassifier.ClassifyDocument  → Same logic, injected via │
│   (extension + filename keywords)       DI                      │
│                                                                 │
│ DataExtractor.ExtractData            → Same logic; read blob    │
│   (reads file from disk)                stream instead of file  │
│                                                                 │
│ PdfParser.ExtractText                → Replace iTextSharp with  │
│   (iTextSharp)                          iText 8 or PdfPig       │
│                                         (.NET 8 compatible)     │
│                                                                 │
│ CsvImporter.ImportCsv                → Same logic with          │
│   (CsvHelper + StreamReader)            CsvHelper (cross-plat)  │
│                                                                 │
│ ComplianceChecker.CheckCompliance    → Same logic, injected     │
│   (DB rule lookup + evaluation)                                 │
│                                                                 │
│ SqlHelper (ADO.NET, static)          → Injected data service    │
│                                         (async, connection from │
│                                         app settings)           │
│                                                                 │
│ EmailNotifier (System.Net.Mail)      → Azure Communication      │
│                                         Services Email SDK      │
│                                                                 │
│ File moved to Processed/Error dir    → Move blob to             │
│                                         processed/ or error/    │
│                                         container/virtual path  │
└─────────────────────────────────────────────────────────────────┘
```

**FileSystemWatcher → Blob Trigger mapping detail:**

| FileSystemWatcher Aspect | Blob Trigger Equivalent |
|--------------------------|------------------------|
| `new FileSystemWatcher(path)` | `[BlobTrigger("incoming/{name}")]` attribute |
| `NotifyFilter = FileName \| LastWrite` | Blob creation detected automatically |
| `Filter = "*.*"` | Path pattern `incoming/{name}` matches all blobs |
| `EnableRaisingEvents = true/false` | Function enabled/disabled via Azure portal or host.json |
| `Created += OnFileCreated` | Function method is the handler |
| `Error += OnError` | Azure Functions retry policy + poison blob handling |
| 500 ms `Thread.Sleep` debounce | Not needed — blob upload is atomic |
| Can miss events under load | Event Grid–based blob trigger guarantees delivery |

### 4.2 ComplianceReporter → Three Timer Trigger Functions

```
┌─────────────────────────────────────────────────────────────────┐
│ CURRENT (Windows Service)            → TARGET (Azure Function)  │
├─────────────────────────────────────────────────────────────────┤
│ System.Timers.Timer (1440 min)       → Timer Trigger            │
│   → DailyComplianceReport               "0 0 6 * * *"          │
│     .GenerateReport()                   → same report logic     │
│     → PdfExporter.ExportToPdf()         → write PDF to blob     │
│     → writes to C:\MeridianData\Reports    (reports/ container) │
│                                                                 │
│ System.Timers.Timer (10080 min)      → Timer Trigger            │
│   → WeeklyRiskReport                    "0 0 7 * * 1"          │
│     .GenerateReport()                   → same report logic     │
│     → ExcelExporter.ExportToExcel()     → write XLSX to blob    │
│     → writes to C:\MeridianData\Reports                        │
│                                                                 │
│ System.Timers.Timer (43200 min)      → Timer Trigger            │
│   → MonthlyAuditReport                  "0 0 8 1 * *"          │
│     .GenerateReport()                   → same report logic     │
│     → PdfExporter + ExcelExporter       → write PDF + XLSX to   │
│     → writes to C:\MeridianData\Reports    blob                 │
└─────────────────────────────────────────────────────────────────┘
```

**System.Timers.Timer → Timer Trigger mapping detail:**

| Timer Aspect | Timer Trigger Equivalent |
|-------------|-------------------------|
| `new Timer(intervalMs)` | `[TimerTrigger("cron-expression")]` attribute |
| `timer.Elapsed += handler` | Function method is the handler |
| `timer.AutoReset = true` | Timer trigger re-fires per CRON schedule |
| `timer.Start()` / `timer.Stop()` | Function enabled/disabled in host.json or portal |
| No distributed coordination | Azure Functions runtime guarantees single-instance execution via blob lease |
| Interval-based (drift-prone) | CRON-based (wall-clock accurate) |
| `DailyReportIntervalMinutes = 1440` | `0 0 6 * * *` (6 AM UTC daily) |
| `WeeklyReportIntervalMinutes = 10080` | `0 0 7 * * 1` (7 AM UTC Monday) |
| `MonthlyReportIntervalMinutes = 43200` | `0 0 8 1 * *` (8 AM UTC 1st of month) |

### 4.3 PortfolioValuation → `RunPortfolioValuation` (Timer Trigger)

```
┌─────────────────────────────────────────────────────────────────┐
│ CURRENT (Windows Service)            → TARGET (Azure Function)  │
├─────────────────────────────────────────────────────────────────┤
│ System.Timers.Timer (60 min)         → Timer Trigger            │
│                                         "0 0 * * * *"           │
│                                                                 │
│ PriceFetcher.FetchLatestPrices()     → Same logic; optionally   │
│   (DB read + mock fallback)             add real HTTP call via   │
│                                         HttpClient + DI         │
│                                                                 │
│ FeedParser.ParsePriceData()          → Wire into PriceFetcher   │
│   (JSON parsing, currently unused)      when API is live        │
│                                                                 │
│ PortfolioCalculator                  → Same calculation logic   │
│   .CalculateValuation()                 via DI                  │
│                                                                 │
│ BenchmarkComparator                  → Wire into pipeline;      │
│   (currently unused)                    same logic via DI       │
│                                                                 │
│ ValuationService.SaveValuation()     → Async data service       │
│   (usp_InsertValuation via SqlHelper)                           │
│                                                                 │
│ ValuationService.CheckThresholds()   → Enqueue alert message    │
│   → AlertService.SendAlert()            to Storage Queue        │
│   → usp_InsertAlert                    (optional decoupling)    │
│                                                                 │
│ AlertService.SendEmailAlert()        → Azure Communication      │
│   (simulated SMTP)                      Services Email SDK      │
└─────────────────────────────────────────────────────────────────┘
```

---

## 5. Shared Library Refactoring

### 5.1 SqlHelper — Data Access Refactoring

| Current | Issue | Target |
|---------|-------|--------|
| `SqlHelper` is static with hardcoded connection string from `ConfigurationManager` | Not compatible with DI, async, or Azure Functions configuration | Refactor to instance class implementing `IDataService` interface; inject via DI; use `IConfiguration` for connection string |
| Synchronous ADO.NET (`ExecuteQuery`, `ExecuteNonQuery`) | Blocks threads; poor for Azure Functions consumption plan | Convert to async methods (`ExecuteQueryAsync`, etc.) using `SqlDataReader.ReadAsync()` |
| `System.Data.SqlClient` | Deprecated for .NET 8 | Replace with `Microsoft.Data.SqlClient` |
| `ConfigurationManager.ConnectionStrings["MeridianDB"]` | Not available in Azure Functions isolated worker | Use `IConfiguration` to read from `local.settings.json` / App Settings |
| Windows Integrated Security | Not available for Azure SQL | Use connection string with password or Azure Managed Identity (`Authentication=Active Directory Managed Identity`) |

**Proposed interface:**

```csharp
public interface IDataService
{
    Task<DataTable> ExecuteQueryAsync(string query);
    Task<int> ExecuteNonQueryAsync(string query);
    Task<DataTable> ExecuteStoredProcedureAsync(string procedureName, params SqlParameter[] parameters);
    Task<int> ExecuteStoredProcedureNonQueryAsync(string procedureName, params SqlParameter[] parameters);
}
```

### 5.2 StoredProcedures — No Change Required

The `StoredProcedures` constants class is portable as-is. No refactoring needed.

### 5.3 Models — Minimal Changes

| Model Class | Change Needed |
|-------------|--------------|
| `Document` | None — POCO is portable |
| `Portfolio`, `Holding`, `Valuation`, `MarketData`, `Alert` | None — POCOs are portable |
| `ComplianceCheck`, `ComplianceRule` | None — POCOs are portable |

### 5.4 Shared Dependencies Summary

All three services depend on `Meridian.Shared`:

| Service | Uses SqlHelper | Uses StoredProcedures | Uses Models |
|---------|---------------|----------------------|-------------|
| DocumentProcessor | ✅ (DataExtractor, ComplianceChecker) | ✅ (InsertDocument, GetActiveComplianceRules) | ✅ (Document) |
| ComplianceReporter | ✅ (all 3 report classes) | ✅ (GetDailyComplianceData, GetWeeklyRiskData, GetMonthlyAuditData) | ✅ (implicit via DataTable) |
| PortfolioValuation | ✅ (ValuationService, PriceFetcher, AlertService) | ✅ (GetPortfolios, GetHoldingsByPortfolio, GetLatestMarketData, InsertValuation, InsertAlert) | ✅ (Portfolio, Holding, MarketData, Alert) |

---

## 6. Infrastructure & Cross-Cutting Mapping

### 6.1 SMTP → Azure Communication Services

| Current (System.Net.Mail) | Target (Azure Communication Services) |
|---------------------------|--------------------------------------|
| `SmtpClient` + `MailMessage` | `EmailClient` from `Azure.Communication.Email` SDK |
| `smtp.office365.com:587` | ACS endpoint (e.g., `https://<resource>.communication.azure.com`) |
| `NetworkCredential` (username/password) | ACS connection string or Managed Identity |
| `SmtpServer`, `SmtpPort` app settings | `ACS_CONNECTION_STRING` app setting |
| `FromEmail = noreply@meridiancapital.com` | Must be a verified sender domain in ACS |
| `ToEmail = compliance@meridiancapital.com` | Same — passed as recipient |
| Used by: `EmailNotifier` (DocProcessor), `AlertService` (PortfolioValuation) | Shared `IEmailService` interface, single implementation |

**Proposed shared email interface:**

```csharp
public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}
```

### 6.2 FileSystemWatcher → Blob Trigger

| FileSystemWatcher (Current) | Azure Blob Trigger (Target) |
|-----------------------------|----------------------------|
| Watches `C:\MeridianData\Incoming` | Blob container: `incoming-documents` |
| `NotifyFilter = FileName \| LastWrite` | Blob creation event (via polling or Event Grid) |
| `Filter = "*.*"` | Path pattern: `incoming/{name}` |
| 500 ms Thread.Sleep debounce | Not needed — blob writes are atomic |
| Can miss events under heavy load | Event Grid subscription guarantees at-least-once delivery |
| File read via `FileStream` | Blob read via `Stream` parameter or `BlobClient` |
| Move to `Processed`/`Error` directory | Copy blob to `processed/` or `error/` container, then delete source |
| `IncomingDocPath`, `ProcessedDocPath`, `ErrorDocPath` config | Container names in app settings |

### 6.3 System.Timers.Timer → Timer Trigger

| System.Timers.Timer (Current) | Azure Functions Timer Trigger (Target) |
|-------------------------------|---------------------------------------|
| Interval in milliseconds | CRON expression (6-field NCrontab) |
| `timer.Elapsed += handler` | `[TimerTrigger("expression")]` attribute on function |
| `timer.Start()` / `timer.Stop()` | Function enable/disable |
| No duplicate protection across instances | Blob lease ensures single execution |
| Interval-based (accumulates drift) | Wall-clock CRON (no drift) |
| All timers in one Windows Service | Separate Azure Functions (independent scaling) |

**CRON expression mapping:**

| Timer | Interval | CRON Expression | Description |
|-------|----------|----------------|-------------|
| Daily compliance report | 1 440 min | `0 0 6 * * *` | Every day at 06:00 UTC |
| Weekly risk report | 10 080 min | `0 0 7 * * 1` | Every Monday at 07:00 UTC |
| Monthly audit report | 43 200 min | `0 0 8 1 * *` | 1st of each month at 08:00 UTC |
| Portfolio valuation | 60 min | `0 0 * * * *` | Every hour on the hour |

### 6.4 Logging: log4net → ILogger / Application Insights

| log4net (Current) | ILogger + App Insights (Target) |
|-------------------|-------------------------------|
| `LogManager.GetLogger(typeof(T))` | Constructor-injected `ILogger<T>` |
| `log.Info(...)`, `log.Error(...)` | `logger.LogInformation(...)`, `logger.LogError(...)` |
| RollingFileAppender → `logs\*.log` | Application Insights (automatic with Azure Functions) |
| EventLogAppender | Not needed in cloud |
| ConsoleAppender | Built-in console logging in Functions |
| `log4net.config` section in App.config | `host.json` logging configuration |

### 6.5 Configuration: App.config → Azure Functions Settings

| Current Mechanism | Target Mechanism |
|-------------------|-----------------|
| `ConfigurationManager.AppSettings["key"]` | `IConfiguration["key"]` via DI |
| `ConfigurationManager.ConnectionStrings["MeridianDB"]` | `IConfiguration.GetConnectionString("MeridianDB")` |
| App.config XML file | `local.settings.json` (local dev) / Azure App Settings (deployed) |
| Hardcoded defaults in code | Environment variables or Azure App Configuration |

### 6.6 Windows Service Hosting: Topshelf → Azure Functions Runtime

| Topshelf (Current) | Azure Functions (Target) |
|--------------------|-----------------------------|
| `HostFactory.Run(...)` | Azure Functions runtime (host builder) |
| `ServiceRecovery` (auto-restart) | Azure Functions automatic retry + consumption plan scaling |
| `RunAsLocalSystem()` | Managed Identity |
| Service install/uninstall commands | Azure deployment (ZIP deploy, CI/CD) |
| Single-instance on one server | Auto-scaled instances with singleton orchestration |

---

## 7. Configuration Migration

### Complete App Setting Migration Matrix

| Service | App.config Key | Current Value | Azure Functions Setting | Notes |
|---------|---------------|---------------|------------------------|-------|
| All | `MeridianDB` (conn string) | `Server=localhost;Database=MeridianCapital;Integrated Security=true;` | `MeridianDB` (connection string) | Change to Azure SQL connection string with Managed Identity |
| DocProcessor | `IncomingDocPath` | `C:\MeridianData\Incoming` | `IncomingBlobContainer` = `incoming-documents` | Blob container replaces folder path |
| DocProcessor | `ProcessedDocPath` | `C:\MeridianData\Processed` | `ProcessedBlobContainer` = `processed-documents` | Blob container for successfully processed files |
| DocProcessor | `ErrorDocPath` | `C:\MeridianData\Error` | `ErrorBlobContainer` = `error-documents` | Blob container for failed files |
| DocProcessor | `SmtpServer` | `smtp.office365.com` | `ACS_CONNECTION_STRING` | Azure Communication Services replaces SMTP |
| DocProcessor | `SmtpPort` | `587` | *(removed)* | Not applicable with ACS SDK |
| DocProcessor | `FromEmail` | `noreply@meridiancapital.com` | `EmailFromAddress` | Must be verified sender in ACS |
| DocProcessor | `ToEmail` | `compliance@meridiancapital.com` | `EmailToAddress` | Recipient address |
| Reporter | `ReportOutputPath` | `C:\MeridianData\Reports` | `ReportsBlobContainer` = `reports` | Blob container for generated reports |
| Reporter | `DailyReportIntervalMinutes` | `1440` | *(removed)* | Replaced by CRON expression in function attribute |
| Reporter | `WeeklyReportIntervalMinutes` | `10080` | *(removed)* | Replaced by CRON expression in function attribute |
| Reporter | `MonthlyReportIntervalMinutes` | `43200` | *(removed)* | Replaced by CRON expression in function attribute |
| Valuation | `ValuationIntervalMinutes` | `60` | *(removed)* | Replaced by CRON expression in function attribute |
| Valuation | `MarketDataApiUrl` | `https://api.marketdata.example.com/prices` | `MarketDataApiUrl` | Keep as app setting; wire into HttpClient |
| Valuation | `DailyReturnThreshold` | `5.0` | `DailyReturnThreshold` = `5.0` | Direct migration |

### New Azure-Specific Settings

| Setting | Value | Purpose |
|---------|-------|---------|
| `AzureWebJobsStorage` | Storage account connection string | Required for blob/timer/queue triggers |
| `ACS_CONNECTION_STRING` | ACS connection string | Azure Communication Services email |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | App Insights connection string | Telemetry and logging |
| `MeridianDB` | Azure SQL connection string | Database connectivity |

---

## 8. Database & Stored Procedure Inventory

### Tables (from `Database/schema.sql`)

| Table | Used By | Record Purpose |
|-------|---------|----------------|
| `Documents` | DocumentProcessor | Incoming document metadata and extracted content |
| `ComplianceRules` | DocumentProcessor | Active compliance rule definitions (CONTAINS, LENGTH expressions) |
| `ComplianceChecks` | DocumentProcessor | Results of compliance rule evaluations per document |
| `Portfolios` | PortfolioValuation | Client portfolio definitions |
| `Holdings` | PortfolioValuation | Security positions per portfolio |
| `Valuations` | PortfolioValuation, ComplianceReporter | Computed NAV and return metrics |
| `MarketData` | PortfolioValuation | Security prices and volume |
| `Alerts` | PortfolioValuation | Threshold breach alerts |

### Stored Procedures

| Stored Procedure | Called By | Operation | Parameters |
|-----------------|-----------|-----------|------------|
| `usp_InsertDocument` | DataExtractor | INSERT | FileName, DocumentType, ReceivedDate, Status, ClientId, ExtractedData |
| `usp_UpdateDocumentStatus` | *(defined, not currently called)* | UPDATE | — |
| `usp_GetDocumentById` | *(defined, not currently called)* | SELECT | — |
| `usp_GetPendingDocuments` | *(defined, not currently called)* | SELECT | — |
| `usp_InsertComplianceCheck` | *(defined, not currently called)* | INSERT | — |
| `usp_GetActiveComplianceRules` | ComplianceChecker | SELECT | *(none)* |
| `usp_GetComplianceChecksByDocument` | *(defined, not currently called)* | SELECT | — |
| `usp_GetDailyComplianceData` | DailyComplianceReport | SELECT | *(none)* |
| `usp_GetWeeklyRiskData` | WeeklyRiskReport | SELECT | *(none)* |
| `usp_GetMonthlyAuditData` | MonthlyAuditReport | SELECT | *(none)* |
| `usp_GetPortfolios` | ValuationService | SELECT | *(none)* |
| `usp_GetHoldingsByPortfolio` | ValuationService | SELECT | @PortfolioId |
| `usp_GetLatestMarketData` | PriceFetcher | SELECT | *(none)* |
| `usp_InsertValuation` | ValuationService | INSERT | @PortfolioId, @AsOfDate, @NAV, @DailyReturn, @MTDReturn, @YTDReturn |
| `usp_InsertMarketData` | *(defined, not currently called)* | INSERT | — |
| `usp_InsertAlert` | AlertService | INSERT | @PortfolioId, @AlertType, @Message, @CreatedDate |

> **Note:** 5 stored procedures are defined in `StoredProcedures.cs` but not actively called. They should be retained for the migration as they may be needed for future features.

---

## 9. NuGet Package Migration

| Current Package | Purpose | .NET 8 Replacement | Notes |
|----------------|---------|-------------------|-------|
| **Topshelf** | Windows Service hosting | *(removed)* | Replaced by Azure Functions runtime |
| **log4net** | Logging | `Microsoft.Extensions.Logging` + `Microsoft.ApplicationInsights` | Use built-in `ILogger<T>` |
| **iTextSharp** (5.x) | PDF parsing & generation | `iText` 8.x or `PdfPig` (for reading); `QuestPDF` or `iText` 8 (for writing) | iTextSharp is AGPL; check licensing |
| **CsvHelper** | CSV parsing | `CsvHelper` (latest) | Cross-platform, fully compatible with .NET 8 |
| **EPPlus** (4.x) | Excel generation | `EPPlus` 7.x (commercial) or `ClosedXML` (MIT) | EPPlus 5+ requires a license; ClosedXML is free |
| **Newtonsoft.Json** | JSON parsing | `System.Text.Json` (built-in) or keep `Newtonsoft.Json` | `System.Text.Json` preferred for new code |
| **System.Data.SqlClient** | Database access | `Microsoft.Data.SqlClient` | Required for Azure SQL + Managed Identity support |
| *(new)* | Email notifications | `Azure.Communication.Email` | Replaces System.Net.Mail SMTP approach |
| *(new)* | Blob storage access | `Azure.Storage.Blobs` | Used by blob trigger binding and explicit blob operations |
| *(new)* | Azure Functions SDK | `Microsoft.Azure.Functions.Worker` + `Microsoft.Azure.Functions.Worker.Sdk` | Isolated worker model for .NET 8 |

---

## 10. Risk & Considerations

### High Priority

| Risk | Description | Mitigation |
|------|-------------|------------|
| **Blob trigger latency** | Default polling-based blob trigger can have up to 10-minute delay | Use **Event Grid–based blob trigger** for near-real-time (<1 s) processing |
| **iTextSharp licensing** | iTextSharp 5.x is AGPL; iText 7+/8 is commercial | Evaluate `PdfPig` (Apache 2.0) for reading, `QuestPDF` (MIT) for writing |
| **EPPlus licensing** | EPPlus 5+ requires a commercial license for commercial use | Evaluate `ClosedXML` (MIT) as a drop-in replacement |
| **SQL connection security** | Current code uses Windows Integrated Security | Use Azure Managed Identity or connection string with AAD authentication |
| **Static SqlHelper** | Cannot be injected via DI; tight coupling | Refactor to instance-based `IDataService` before migration |

### Medium Priority

| Risk | Description | Mitigation |
|------|-------------|------------|
| **Data truncation** | `DataExtractor` truncates extracted data to 4 000 chars | Review if `NVARCHAR(MAX)` column needs the limit; remove or increase |
| **Mock implementations** | `PortfolioCalculator` returns random returns; `BenchmarkComparator` has mock data | Flag for replacement with real calculation logic during or after migration |
| **Unused components** | `FeedParser` and `BenchmarkComparator` are implemented but not wired | Decide whether to wire in during migration or defer |
| **Hardcoded ClientId** | `DataExtractor` uses `ClientId = 1` | Parameterize or derive from blob metadata |
| **File-to-blob path mapping** | Code references local paths (`C:\MeridianData\*`) throughout | Search-and-replace all `System.IO` file operations with blob SDK equivalents |
| **Synchronous database calls** | All `SqlHelper` methods are synchronous | Convert to async to avoid thread pool starvation in Azure Functions |

### Low Priority

| Risk | Description | Mitigation |
|------|-------------|------------|
| **Report file naming** | Reports use `DateTime.Now` for filenames | Use UTC consistently (`DateTime.UtcNow`) in cloud |
| **log4net config in App.config** | Complex log4net XML configuration | Remove entirely; configure logging in `host.json` and `Program.cs` |
| **5 unused stored procedures** | Defined in `StoredProcedures.cs` but never called | Retain in migration; document for future use |
