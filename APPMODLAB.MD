---
title: "Windows Services to Azure Functions"
description: "Convert Windows Services to serverless Azure Functions with blob, timer & queue triggers"
authors: ["marconsilva"]
category: "Infra Modernization"
industry: "Financial Services"
services: ["Azure Functions", "Azure Blob Storage", "Azure Queue Storage", "Azure SQL Database", "Application Insights"]
languages: ["C#", ".NET"]
frameworks: ["Azure Functions", "Durable Functions"]
modernizationTools: []
agenticTools: []
tags: ["windows-services", "serverless", "azure-functions", "durable-functions", "event-driven"]
extensions: ["github.copilot"]
thumbnail: ""
video: ""
version: "1.0.0"
---

## Legacy Application Overview

Meridian Capital Advisors runs three monolithic .NET Framework 4.8 Windows Services for financial document processing, compliance reporting, and portfolio valuation. These always-on services use FileSystemWatcher (which misses events under load), System.Timers.Timer (with no distributed coordination), and static ADO.NET helpers — all targeted for migration to event-driven Azure Functions.

### Solution Architecture

The Visual Studio solution contains three Windows Service projects plus a shared library, organized in a classic .NET Framework structure with ServiceBase-derived entry points.

![Solution Structure — Three Windows Service projects (DocumentProcessor, ComplianceReporter, PortfolioValuation) plus shared library](assets/screenshots/01-solution-structure.png)

### Build Verification

The legacy solution builds successfully under .NET Framework 4.8, confirming all dependencies and project references are intact before beginning the migration.

![Build Output — Successful .NET Framework 4.8 build confirming all project dependencies](assets/screenshots/02-build-output.png)

### Document Processor Service

The DocumentProcessor service watches a file system directory for incoming financial documents, parses them, and stores results in SQL Server. Under load, the FileSystemWatcher can silently miss events.

![Document Processor Service — FileSystemWatcher-based document ingestion pipeline](assets/screenshots/03-document-processor-service.png)

### Compliance Reporter Service

The ComplianceReporter runs on a timer schedule, generating regulatory compliance reports from SQL Server data and distributing them via SMTP.

![Compliance Reporter Service — Timer-scheduled compliance report generation](assets/screenshots/04-compliance-reporter-service.png)

### Portfolio Valuation Service

The PortfolioValuation service performs scheduled portfolio recalculations, aggregating market data and computing NAV figures for client accounts.

![Portfolio Valuation Service — Scheduled portfolio NAV recalculation engine](assets/screenshots/05-portfolio-valuation-service.png)

### Shared Library & Database

The shared library provides common data access, models, and database schema used across all three services.

![Shared Library and Database — Common data access layer and SQL Server schema](assets/screenshots/06-shared-library-database.png)

### Configuration Comparison

Each service has its own App.config with connection strings, timer intervals, and file paths — configuration sprawl that will be consolidated into Azure Function app settings.

![App Configs — Per-service App.config files with connection strings and timer settings](assets/screenshots/07-app-configs.png)

### Migration Mapping

The migration mapping shows how each legacy Windows Service component maps to its Azure Functions equivalent: FileSystemWatcher → Blob triggers, Timers → Timer triggers, MSMQ → Queue triggers.

![Migration Mapping — Legacy Windows Service components mapped to Azure Functions triggers](assets/screenshots/08-migration-mapping.png)

---

## Screenshots

### Solution Structure
![Solution Structure](assets/screenshots/01-solution-structure.png)

### Build Output
![Build Output](assets/screenshots/02-build-output.png)

### Document Processor Service
![Document Processor Service](assets/screenshots/03-document-processor-service.png)

### Compliance Reporter Service
![Compliance Reporter Service](assets/screenshots/04-compliance-reporter-service.png)

### Portfolio Valuation Service
![Portfolio Valuation Service](assets/screenshots/05-portfolio-valuation-service.png)

### Shared Library & Database Schema
![Shared Library and Database](assets/screenshots/06-shared-library-database.png)

### App.config Comparison
![App Configs](assets/screenshots/07-app-configs.png)

### Migration Mapping (Legacy → Azure Functions)
![Migration Mapping](assets/screenshots/08-migration-mapping.png)
