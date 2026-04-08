# ⚡ WINDOWS SERVICES → AZURE FUNCTIONS 🌩️

```
╔══════════════════════════════════════════════════════════════╗
║  🎮 SERVICE STOPPED ⏹️  → FUNCTION TRIGGERED ⚡             ║
║  💾 ALWAYS-ON SERVERS  → PAY-PER-EXECUTION 💰               ║
║  🔄 MONOLITHIC TIMERS  → EVENT-DRIVEN MAGIC ✨              ║
║  ❄️  COLD START...     → 🔥 BLAZING FAST SCALE              ║
╚══════════════════════════════════════════════════════════════╝
```

## 🌟 OVERVIEW

**GOING SERVERLESS 🌩️** — Time to unplug those always-on Windows Services and migrate to the cloud-native, event-driven world of Azure Functions! 

This lab takes you through the journey of modernizing **Meridian Capital Advisors**, a financial services company running three monolithic Windows Services on dusty on-premises servers 🖥️💨. We're transforming their financial document processing, compliance reporting, and portfolio valuation systems into sleek, serverless Azure Functions that scale on-demand and cost pennies per million executions! 💸

**Legacy Stack:** 
- 🪟 .NET Framework 4.8 Windows Services (ServiceBase)
- 📂 FileSystemWatcher (missing events under load!)
- ⏰ System.Timers.Timer (no distributed coordination!)
- 🐌 Static ADO.NET helpers
- 📧 SMTP with no connection pooling
- 🪵 Log4Net XML sprawl

**Target Stack:**
- ⚡ Azure Functions v4 on .NET 9 (isolated worker)
- 📦 Blob triggers (FileSystemWatcher → event grid magic!)
- ⏱️ Timer triggers (scheduled tasks, done right!)
- 📨 Queue triggers (decoupled pipeline stages!)
- 🔗 Durable Functions (orchestration with retry!)
- 📊 Application Insights (observable by default!)
- 💌 Azure Communication Services (modern email!)

## 🎯 WHAT YOU'LL LEARN

By the end of this lab, you'll master:

- 🔍 **Service Decomposition** — Breaking monolithic Windows Services into discrete functions
- 🎲 **Trigger Selection** — Choosing the right trigger (blob, timer, queue, HTTP) for each workload
- 🎭 **Durable Orchestration** — Multi-step workflows with automatic retry and compensation
- 📦 **Blob Event Processing** — From fragile FileSystemWatcher to reliable blob triggers
- 📊 **Serverless Monitoring** — Application Insights, metrics, and alerts for production-grade observability
- 💰 **Cost Optimization** — Pay-per-execution vs. always-on infrastructure
- 🚀 **Serverless Patterns** — Queue-based pipelines, fan-out/fan-in, and stateful workflows

## 🎮 PREREQUISITES

Before you start your serverless journey, make sure you have:

- ✅ **C# & .NET Experience** — Comfortable with C# and .NET development
- ✅ **Azure Functions Basics** — Understand the fundamentals of serverless computing
- ✅ **.NET 9 SDK** — [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)
- ✅ **Azure Functions Core Tools v4** — [Install here](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- ✅ **Azure Subscription** — [Get a free account](https://azure.microsoft.com/free/)
- ✅ **Visual Studio 2022 or VS Code** — With Azure Functions extension
- ✅ **Azure CLI** — [Install here](https://learn.microsoft.com/cli/azure/install-azure-cli)
- ✅ **GitHub Copilot CLI** — For AI-powered development assistance

## 🚀 QUICK START

```bash
# Clone the repo 📥
git clone https://github.com/EmeaAppGbb/appmodlab-windows-services-to-azure-functions.git
cd appmodlab-windows-services-to-azure-functions

# Checkout the legacy branch to see the Windows Services 🪟
git checkout legacy

# Restore dependencies 📦
dotnet restore

# Run the services as console apps (for development) 🏃
dotnet run --project Meridian.DocumentProcessor
dotnet run --project Meridian.ComplianceReporter
dotnet run --project Meridian.PortfolioValuation

# Ready to modernize? Start the lab! ⚡
# Follow the step-by-step guide in APPMODLAB.md
```

## 📂 PROJECT STRUCTURE

```
appmodlab-windows-services-to-azure-functions/
├── 📖 APPMODLAB.md                        # Complete lab guide with step-by-step instructions
├── 📖 README.md                           # You are here! 👋
├── 🪟 MeridianDocProcessor/               # Legacy Windows Services (legacy branch)
│   ├── Meridian.DocumentProcessor/        # Service 1: Document processing
│   │   ├── DocumentProcessorService.cs    # ServiceBase with OnStart/OnStop
│   │   ├── FileWatcher/
│   │   │   ├── IncomingDocWatcher.cs      # FileSystemWatcher (replace with blob trigger!)
│   │   │   ├── PdfParser.cs               # PDF text extraction
│   │   │   └── CsvImporter.cs            # CSV financial data import
│   │   ├── Processing/
│   │   │   ├── DocumentClassifier.cs      # Classifies document type
│   │   │   ├── DataExtractor.cs           # Extracts financial data
│   │   │   └── ComplianceChecker.cs       # Validates against rules
│   │   └── Notifications/
│   │       └── EmailNotifier.cs           # SMTP notifications
│   ├── Meridian.ComplianceReporter/       # Service 2: Scheduled compliance reports
│   │   ├── ComplianceReporterService.cs   # Timer-based report generation
│   │   ├── Reports/
│   │   │   ├── DailyComplianceReport.cs   # Daily EOD report
│   │   │   ├── WeeklyRiskReport.cs        # Weekly risk summary
│   │   │   └── MonthlyAuditReport.cs      # Monthly audit trail
│   │   └── Exporters/
│   │       ├── PdfExporter.cs             # Report to PDF
│   │       └── ExcelExporter.cs           # Report to Excel
│   └── Meridian.PortfolioValuation/       # Service 3: Portfolio valuations
│       ├── ValuationService.cs            # Timer-based portfolio updates
│       ├── MarketDataFeed/
│       │   ├── PriceFetcher.cs            # HTTP calls to market data provider
│       │   └── FeedParser.cs             # Parse market data responses
│       ├── Calculations/
│       │   ├── PortfolioCalculator.cs     # NAV, returns, risk calculations
│       │   └── BenchmarkComparator.cs     # Compare against benchmarks
│       └── Notifications/
│           └── AlertService.cs            # Alert on threshold breaches
├── ⚡ MeridianFunctions/                  # Azure Functions solution (solution branch)
│   ├── DocumentProcessing/
│   │   ├── BlobTriggerDocumentReceived.cs # Blob trigger → Queue
│   │   ├── QueueTriggerClassifyDoc.cs     # Queue trigger → Classification
│   │   ├── QueueTriggerExtractData.cs     # Queue trigger → Extraction
│   │   └── DurableOrchestration.cs        # Orchestrator for multi-step pipeline
│   ├── ComplianceReporting/
│   │   ├── TimerDailyCompliance.cs        # Timer trigger: 0 0 18 * * * (6 PM daily)
│   │   ├── TimerWeeklyRisk.cs             # Timer trigger: 0 0 9 * * MON
│   │   ├── TimerMonthlyAudit.cs           # Timer trigger: 0 0 6 1 * *
│   │   └── HttpOnDemandReport.cs          # HTTP trigger for ad-hoc reports
│   └── PortfolioValuation/
│       ├── TimerMarketDataFetch.cs        # Timer trigger: 0 */30 9-16 * * MON-FRI
│       ├── TimerPortfolioValuation.cs     # Timer trigger: 0 0 17 * * MON-FRI
│       └── QueueTriggerSendAlerts.cs      # Queue trigger for threshold alerts
├── 🏗️ Infrastructure/
│   ├── main.bicep                         # Main Bicep template
│   ├── modules/
│   │   ├── function-app.bicep             # Azure Functions (Consumption plan)
│   │   ├── storage.bicep                  # Storage Account (blobs + queues)
│   │   ├── sql.bicep                      # Azure SQL Database
│   │   ├── app-insights.bicep             # Application Insights
│   │   └── communication.bicep            # Azure Communication Services
│   └── parameters.json                    # Environment parameters
├── 🗄️ Database/
│   ├── Schema/                            # Database schema scripts
│   └── StoredProcedures/                  # 30+ stored procedures
└── 🚀 .github/
    └── workflows/
        └── deploy-functions.yml           # CI/CD for Azure Functions
```

## 🪟 LEGACY STACK: THE ALWAYS-ON ERA

**Three Windows Services running 24/7 on dedicated servers:**

### 🔷 Service 1: Document Processor
**What it does:** Monitors a network folder for incoming financial documents (PDFs, CSVs), classifies them, extracts data, runs compliance checks, and sends email notifications.

**How it works:**
- `FileSystemWatcher` monitors `\\fileserver\incoming\` (fragile! misses events under load 😱)
- `OnCreated` event → parse document → classify → extract → validate → email
- No retry logic — failed processing is silently ignored 🤐
- Static `SqlHelper` with hardcoded connection strings 🔐💀

**Anti-patterns:**
- ❌ FileSystemWatcher unreliability
- ❌ No poison message handling
- ❌ Synchronous processing blocks further events
- ❌ No distributed tracing or correlation

### 🔶 Service 2: Compliance Reporter
**What it does:** Generates scheduled compliance reports (daily, weekly, monthly) and exports them to PDF/Excel.

**How it works:**
- `System.Timers.Timer` triggers at configured intervals
- Queries database for compliance data
- Generates reports using hardcoded templates
- Exports to network share `\\fileserver\reports\`
- Emails stakeholders via SMTP

**Anti-patterns:**
- ❌ No distributed timer coordination (multiple instances = duplicate reports!)
- ❌ Timer drift and lost events on service restart
- ❌ SMTP client created per email (no connection pooling)
- ❌ No observability into report generation success/failure

### 🔷 Service 3: Portfolio Valuation
**What it does:** Fetches market data from external APIs, calculates portfolio valuations, and sends alerts on threshold breaches.

**How it works:**
- `System.Timers.Timer` triggers every 30 minutes during market hours
- HTTP calls to market data provider (no retry on transient failures!)
- Calculates NAV, returns, risk metrics
- Stores valuations in database
- Sends threshold breach alerts via email

**Anti-patterns:**
- ❌ No retry on market data API failures
- ❌ No circuit breaker for downstream dependencies
- ❌ Calculations run serially (can't scale horizontally)
- ❌ Manual server patching and maintenance

## ⚡ TARGET ARCHITECTURE: GOING SERVERLESS!

**EVENT-DRIVEN 🎯 | PAY-PER-EXECUTION 💰 | INFINITELY SCALABLE 🚀**

### 🔥 Architecture Overview

```
📦 Azure Blob Storage (documents)
   ↓ [blob trigger]
⚡ DocumentReceivedFunction
   ↓ [queue message: classify]
📨 Azure Queue Storage
   ↓ [queue trigger]
⚡ ClassifyDocumentFunction
   ↓ [queue message: extract]
⚡ ExtractDataFunction
   ↓ [queue message: validate]
⚡ ComplianceCheckFunction
   ↓
💌 Azure Communication Services (email)

⏰ Timer Trigger (CRON: 0 0 18 * * *)
   ↓
⚡ DailyComplianceReportFunction
   ↓
📊 Generate PDF → Blob Storage → Email

⏰ Timer Trigger (CRON: 0 0 17 * * MON-FRI)
   ↓
⚡ PortfolioValuationFunction
   ↓
📈 Fetch Market Data → Calculate NAV → Store → Alert
```

### 🎭 Durable Functions Orchestration

**Multi-step document processing with retry and compensation:**

```
⚡ Orchestrator: ProcessDocumentWorkflow
   ├── 🎬 Activity: ClassifyDocument (retry 3x with exp backoff)
   ├── 🎬 Activity: ExtractData (retry 3x with exp backoff)
   ├── 🎬 Activity: RunComplianceChecks (retry 3x)
   ├── 🎬 Activity: StoreResults (with idempotency)
   └── 🎬 Activity: SendNotification
```

**Benefits:**
- ✅ Automatic retry with exponential backoff
- ✅ Durable state across restarts
- ✅ Built-in correlation and tracing
- ✅ Human interaction patterns (approvals!)
- ✅ Fan-out/fan-in for parallel processing

### 🎯 Trigger Mapping

| Legacy Component | Azure Function Trigger | Why? |
|-----------------|----------------------|------|
| FileSystemWatcher | **Blob Trigger** 📦 | Event Grid ensures reliable, at-least-once delivery |
| System.Timers.Timer (reports) | **Timer Trigger** ⏰ | Distributed coordination, no duplicate runs |
| System.Timers.Timer (valuation) | **Timer Trigger** ⏱️ | CRON expressions for flexible scheduling |
| Document pipeline | **Queue Triggers** 📨 | Decoupled stages, built-in retry, poison messages |
| Ad-hoc reports | **HTTP Trigger** 🌐 | On-demand execution with API Gateway |
| Multi-step workflows | **Durable Orchestration** 🎭 | Stateful workflows with retry/compensation |

### 📊 Monitoring & Observability

**Application Insights FTW! 📈**

- ✅ **Automatic telemetry** — Every function execution logged
- ✅ **Distributed tracing** — Correlation across function calls
- ✅ **Live metrics** — Real-time dashboard of executions
- ✅ **Custom metrics** — Document processing times, compliance scores
- ✅ **Alerts** — Failures, slow executions, threshold breaches
- ✅ **Log Analytics** — KQL queries for deep insights

## 🛠️ LAB WALKTHROUGH USING COPILOT CLI

This lab is designed to be completed with **GitHub Copilot CLI** as your AI pair programmer! 🤖

### 🎮 Step 1: Explore the Legacy Windows Services
**Duration:** 30 minutes ⏱️

```bash
# Checkout the legacy branch
git checkout legacy

# Ask Copilot to explain the Windows Services architecture
gh copilot suggest "how do I run these Windows Services as console apps?"

# Run each service and observe the behavior
dotnet run --project Meridian.DocumentProcessor

# Use Copilot to understand the FileSystemWatcher implementation
gh copilot explain "what does IncomingDocWatcher.cs do?"
```

**🎯 Goal:** Understand the legacy architecture, identify pain points, and map service components to potential Azure Functions triggers.

### ⚡ Step 2: Map Services to Functions
**Duration:** 45 minutes ⏱️

```bash
# Checkout the step-1-decompose branch
git checkout step-1-decompose

# Ask Copilot to help map Windows Service logic to functions
gh copilot suggest "how should I decompose a Windows Service FileSystemWatcher into Azure Functions?"

# Document your mapping in a design doc
gh copilot suggest "create a trigger selection decision tree for this migration"
```

**🎯 Goal:** Create a mapping document that shows which Windows Service components become which Azure Functions with which triggers.

### 📦 Step 3: Build Blob-Triggered Functions
**Duration:** 60 minutes ⏱️

```bash
# Checkout the step-2-blob-functions branch
git checkout step-2-blob-functions

# Ask Copilot to scaffold a blob-triggered function
gh copilot suggest "create an Azure Function with blob trigger for document processing"

# Implement the document received function
# Ask Copilot for best practices
gh copilot explain "what are best practices for blob trigger bindings?"
```

**🎯 Goal:** Replace FileSystemWatcher with reliable blob triggers, implement queue-based pipeline stages.

### 📨 Step 4: Build Queue Pipeline
**Duration:** 60 minutes ⏱️

```bash
# Implement queue-triggered functions for the pipeline
gh copilot suggest "create queue-triggered functions for document classification, extraction, and validation"

# Add poison message handling
gh copilot explain "how do I handle poison messages in Azure Functions queue triggers?"
```

**🎯 Goal:** Build a decoupled, scalable pipeline with queue triggers, retry policies, and poison message handling.

### ⏰ Step 5: Build Timer Functions
**Duration:** 45 minutes ⏱️

```bash
# Checkout the step-3-timer-functions branch
git checkout step-3-timer-functions

# Ask Copilot to create timer-triggered functions
gh copilot suggest "create timer-triggered Azure Functions for daily compliance reports"

# Learn about CRON expressions
gh copilot explain "what CRON expression means 0 0 18 * * *?"
```

**🎯 Goal:** Replace System.Timers.Timer with robust timer-triggered functions using CRON expressions.

### 🎭 Step 6: Add Durable Orchestration
**Duration:** 90 minutes ⏱️

```bash
# Checkout the step-4-durable-functions branch
git checkout step-4-durable-functions

# Ask Copilot about Durable Functions
gh copilot suggest "create a Durable Functions orchestrator for multi-step document processing"

# Implement retry policies
gh copilot explain "how do I configure retry policies in Durable Functions?"
```

**🎯 Goal:** Implement stateful orchestration for multi-step document processing with automatic retry and compensation.

### 📊 Step 7: Add Monitoring
**Duration:** 30 minutes ⏱️

```bash
# Configure Application Insights
gh copilot suggest "how do I enable Application Insights for Azure Functions?"

# Add custom telemetry
gh copilot suggest "how do I log custom metrics in Azure Functions?"
```

**🎯 Goal:** Set up Application Insights, create custom dashboards, and configure alerts for production monitoring.

### 🚀 Step 8: Deploy to Azure
**Duration:** 60 minutes ⏱️

```bash
# Checkout the step-5-deploy branch
git checkout step-5-deploy

# Review the Bicep infrastructure
gh copilot explain "what does main.bicep deploy?"

# Deploy using Azure CLI
gh copilot suggest "how do I deploy Azure Functions using Bicep?"

# Test the deployed functions
gh copilot suggest "how do I test my blob-triggered function in Azure?"
```

**🎯 Goal:** Deploy the complete solution to Azure, configure all supporting services, and verify end-to-end functionality.

## ⏱️ DURATION

**Total Lab Time:** 4–6 hours 🕐

- 🎮 **Beginner-friendly:** 6 hours (includes extra exploration and Copilot learning)
- ⚡ **Intermediate:** 4-5 hours (some Azure Functions experience)
- 🚀 **Advanced:** 4 hours (familiar with serverless patterns)

**Recommended Approach:** Take breaks between steps! ☕ This is a marathon, not a sprint. The serverless transformation is worth the journey! 🎯

## 📚 RESOURCES

### 🌐 Microsoft Learn

- [Azure Functions Overview](https://learn.microsoft.com/azure/azure-functions/functions-overview)
- [Durable Functions Concepts](https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-overview)
- [Blob Storage Trigger](https://learn.microsoft.com/azure/azure-functions/functions-bindings-storage-blob-trigger)
- [Timer Trigger](https://learn.microsoft.com/azure/azure-functions/functions-bindings-timer)
- [Queue Storage Trigger](https://learn.microsoft.com/azure/azure-functions/functions-bindings-storage-queue-trigger)
- [Application Insights for Functions](https://learn.microsoft.com/azure/azure-functions/functions-monitoring)
- [Azure Functions Best Practices](https://learn.microsoft.com/azure/azure-functions/functions-best-practices)

### 🎓 Patterns & Practices

- [Serverless Patterns Collection](https://serverlessland.com/patterns)
- [Cloud Design Patterns](https://learn.microsoft.com/azure/architecture/patterns/)
- [Retry Pattern](https://learn.microsoft.com/azure/architecture/patterns/retry)
- [Queue-Based Load Leveling](https://learn.microsoft.com/azure/architecture/patterns/queue-based-load-leveling)
- [Strangler Fig Pattern (for migration)](https://learn.microsoft.com/azure/architecture/patterns/strangler-fig)

### 🔧 Tools & SDKs

- [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- [Azure Storage Explorer](https://azure.microsoft.com/features/storage-explorer/)
- [Durable Functions Monitor](https://github.com/microsoft/DurableFunctionsMonitor)
- [Application Insights Profiler](https://learn.microsoft.com/azure/azure-monitor/profiler/profiler-overview)

### 🎬 Videos & Workshops

- [Serverless with Azure Functions (Microsoft Learn)](https://learn.microsoft.com/training/paths/create-serverless-applications/)
- [Durable Functions Deep Dive](https://www.youtube.com/results?search_query=azure+durable+functions)
- [Migrating to Serverless Workshop](https://github.com/Azure-Samples/azure-functions-migration)

### 💬 Community

- [Azure Functions GitHub](https://github.com/Azure/Azure-Functions)
- [Serverless Stack Overflow](https://stackoverflow.com/questions/tagged/azure-functions)
- [Azure Functions Discord](https://discord.gg/azure)

---

## 🎊 READY TO GO SERVERLESS?

```
╔════════════════════════════════════════════════════════════╗
║                                                            ║
║   🎮 PRESS START TO BEGIN YOUR SERVERLESS JOURNEY! 🚀     ║
║                                                            ║
║   💾 SAVE STATE: git checkout legacy                      ║
║   ⚡ LOAD LEVEL 1: Open APPMODLAB.md                      ║
║   🌟 POWER UP: Use GitHub Copilot CLI                     ║
║   🏆 ACHIEVEMENT: From always-on to event-driven!         ║
║                                                            ║
╚════════════════════════════════════════════════════════════╝
```

**Questions? Issues? Found a bug? 🐛**
Open an issue on the GitHub repo or reach out to the Azure App Innovation GBB team!

**Built with 💜 by the EMEA App Innovation Global Black Belt Team**

---

### 🎯 FINAL BOSS CHECKLIST

Before you complete this lab, make sure you can:

- [ ] ⚡ Explain the benefits of serverless vs. always-on services
- [ ] 🎯 Choose the right trigger type for any workload
- [ ] 🎭 Implement Durable Functions orchestration with retry
- [ ] 📦 Use blob triggers for reliable event processing
- [ ] ⏰ Configure timer triggers with CRON expressions
- [ ] 📨 Build queue-based pipelines with poison message handling
- [ ] 📊 Monitor and troubleshoot functions with Application Insights
- [ ] 🚀 Deploy serverless solutions to Azure with IaC
- [ ] 💰 Calculate cost savings from pay-per-execution pricing
- [ ] 🌩️ **Feel the serverless power!** ⚡

**COLD START... ❄️ → 🔥 BLAZING FAST! LET'S GOOOO! 🚀🌩️⚡**
