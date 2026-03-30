# Contoso Bank — Azure SRE Agent Demo

## Project Summary

**Contoso Bank** is a purpose-built online banking portal designed to showcase Azure SRE Agent's ability to investigate, diagnose, and mitigate real-world production issues in seconds. The app looks and feels like a genuine banking application, but specific user actions silently trigger realistic failure modes that SRE Agent can detect and resolve.

Unlike existing Microsoft samples (Grubify/Octopets) which use CLI scripts to inject failures, Contoso Bank embeds failure triggers into natural user workflows — making demos feel organic and convincing.

### What Makes This Demo Unique

1. **Natural failure injection** — Chaos scenarios are triggered by normal-looking banking actions (not a separate chaos panel or CLI script)
2. **Comprehensive observability** — OpenTelemetry unified pipeline exports to both Azure Monitor AND Prometheus/Grafana, demonstrating SRE Agent's ability to connect to any data source via MCP
3. **8 distinct failure types** — Each produces different telemetry signatures for SRE Agent to investigate
4. **One-command deployment** — `azd up` deploys the entire stack (app + infra + monitoring + dashboards)
5. **Banking domain** — Relatable to enterprise audiences in financial services

---

## Tech Stack

| Layer | Technology | Azure Service |
|-------|-----------|---------------|
| Frontend | ASP.NET Core Razor Pages (HTML/CSS/JS) | — |
| Backend | ASP.NET Core Web API (C# / .NET 8) | Azure Container Apps |
| Database | Entity Framework Core | Azure SQL Database |
| Observability | OpenTelemetry SDK (unified logs, metrics, traces) | — |
| Traces + Logs | Azure Monitor OpenTelemetry exporter | App Insights + Log Analytics |
| Metrics | OpenTelemetry Prometheus exporter (`/metrics`) | Azure Monitor Managed Prometheus |
| Dashboards | Azure Managed Grafana | Grafana dashboards + MCP endpoint |
| IaC | Bicep + Azure Developer CLI (azd) | One-command deployment |

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Azure Container Apps                      │
│  ┌─────────────────────────────────────────────────────┐    │
│  │              Contoso Bank Web App                     │    │
│  │  ┌──────────────┐    ┌───────────────────────────┐  │    │
│  │  │ Razor Pages  │───▶│  ASP.NET Core Web API     │  │    │
│  │  │ (Frontend)   │    │  Controllers + Services   │  │    │
│  │  └──────────────┘    │  ChaosService (DI)        │  │    │
│  │                      │  /metrics (Prometheus)    │  │    │
│  │                      └───────────┬───────────────┘  │    │
│  └──────────────────────────────────┼──────────────────┘    │
│                                     │                        │
└─────────────────────────────────────┼────────────────────────┘
                                      │
                    ┌─────────────────┼──────────────────┐
                    ▼                 ▼                   ▼
           ┌──────────────┐  ┌──────────────┐   ┌──────────────┐
           │  Azure SQL   │  │ App Insights │   │  Managed     │
           │  Database    │  │ + Log        │   │  Grafana +   │
           │              │  │   Analytics  │   │  Prometheus  │
           └──────────────┘  └──────────────┘   └──────────────┘
                                      │                   │
                                      └─────────┬─────────┘
                                                ▼
                                      ┌──────────────────┐
                                      │  Azure SRE Agent │
                                      │  (configured     │
                                      │   separately)    │
                                      └──────────────────┘
```

---

## Banking Features (The "Real" App)

The app has enough real functionality to look convincing during a demo:

1. **Dashboard** — Account overview with balance, recent transactions, quick actions
2. **Accounts** — List of bank accounts (checking, savings, credit) with balances
3. **Transfers** — Transfer money between accounts or to external accounts
4. **Transactions** — Transaction history with search/filter
5. **Reports** — Generate account statements and reports
6. **Settings** — Profile and notification preferences

### Data Model

```
Accounts
├── Id (int, PK)
├── AccountNumber (string, unique)
├── AccountName (string)
├── AccountType (enum: Checking/Savings/Credit)
├── Balance (decimal)
├── Currency (string, default "USD")
└── CreatedAt (datetime)

Transactions
├── Id (int, PK)
├── AccountId (int, FK → Accounts)
├── Type (enum: Credit/Debit)
├── Amount (decimal)
├── Description (string)
├── Category (string)
├── Timestamp (datetime)
└── Status (enum: Completed/Pending/Failed)

Transfers
├── Id (int, PK)
├── FromAccountId (int, FK → Accounts)
├── ToAccountId (int, FK → Accounts)
├── Amount (decimal)
├── Status (enum: Processing/Completed/Failed)
├── RequestedAt (datetime)
└── CompletedAt (datetime?)
```

---

## Chaos Scenarios — Failure Buttons Mapped to Natural Actions

Each scenario is triggered by a normal-looking banking action. The user clicks a button that looks legitimate, but it activates a specific failure mode behind the scenes. Each failure generates distinct telemetry signals that SRE Agent can investigate.

### Scenario 1: Memory Leak

| Aspect | Detail |
|--------|--------|
| **User Action** | Click "Generate Annual Statement" on Reports page |
| **What User Sees** | Loading spinner → eventual timeout or error |
| **What Happens** | Backend allocates large byte arrays in a loop, holds references so GC can't reclaim. Memory climbs steadily. |
| **Telemetry Signals** | Memory metric spike in Prometheus (`process_working_set_bytes`), container restart (OOM kill), App Insights `OutOfMemoryException` |
| **SRE Agent Finds** | Memory growth pattern, OOM kill in container logs, correlates to `/api/reports/annual-statement` endpoint |

### Scenario 2: CPU Spike

| Aspect | Detail |
|--------|--------|
| **User Action** | Click "Run Fraud Detection" on Dashboard |
| **What User Sees** | "Scanning transactions..." with spinning indicator |
| **What Happens** | Backend spawns CPU-intensive work — tight loop computing hash permutations (simulating ML fraud model). Pins all CPU cores. |
| **Telemetry Signals** | CPU > 95% in Prometheus (`process_cpu_seconds_total`), increased response latency across all endpoints |
| **SRE Agent Finds** | CPU saturation correlated to fraud-detection endpoint, thread starvation affecting other requests |

### Scenario 3: HTTP 500 Errors

| Aspect | Detail |
|--------|--------|
| **User Action** | Click "Process Wire Transfer" on Transfers page |
| **What User Sees** | Transfer form submission → "Transfer failed" error |
| **What Happens** | Sets an in-memory flag that makes the TransfersController throw `InvalidOperationException` on every subsequent request for 5 minutes. Simulates a broken downstream payment processor. |
| **Telemetry Signals** | HTTP 5xx spike in App Insights, error rate metric in Prometheus, failed request traces |
| **SRE Agent Finds** | Spike in 500 errors isolated to `/api/transfers/*`, exception details in traces, timeline correlation |

### Scenario 4: Database Connection Failure

| Aspect | Detail |
|--------|--------|
| **User Action** | Click "Refresh" on Account Balance |
| **What User Sees** | "Unable to load account information" error |
| **What Happens** | Temporarily swaps the EF Core connection string to an invalid one (wrong password or non-existent server). All DB queries fail. Reverts after 5 minutes. |
| **Telemetry Signals** | `SqlException` flood in App Insights, dependency failure metrics, Prometheus `contosobank_db_errors_total` counter spike |
| **SRE Agent Finds** | Database connectivity failure, SQL error codes, dependency map showing all DB calls failing, connection string issue |

### Scenario 5: Slow API / High Latency

| Aspect | Detail |
|--------|--------|
| **User Action** | Click "International Transfer" on Transfers page |
| **What User Sees** | Very slow loading, eventually completes after 30+ seconds |
| **What Happens** | Injects `Task.Delay(30_000)` into the transfer processing pipeline. Simulates slow external SWIFT/correspondent bank API. |
| **Telemetry Signals** | P99 latency spike in App Insights, Prometheus histogram `contosobank_request_duration_seconds` shows 30s+ bucket, request queue growing |
| **SRE Agent Finds** | Latency spike on specific endpoint, no CPU/memory issues → points to external dependency or artificial delay |

### Scenario 6: Dependency Timeout

| Aspect | Detail |
|--------|--------|
| **User Action** | Click "Verify Identity (KYC)" on Settings page |
| **What User Sees** | "Verifying your identity..." spinner that never completes |
| **What Happens** | Makes an HTTP call to a non-routable IP (e.g., `10.255.255.1`) with a long timeout. Ties up threads. Multiple clicks exhaust the thread pool. |
| **Telemetry Signals** | HttpClient timeout exceptions, Prometheus `contosobank_dependency_timeouts_total`, thread pool exhaustion metrics |
| **SRE Agent Finds** | External dependency unresponsive, cascading timeout failure, thread starvation pattern |

### Scenario 7: Disk/Log Flooding

| Aspect | Detail |
|--------|--------|
| **User Action** | Click "Export Full Transaction History" on Transactions page |
| **What User Sees** | "Preparing export..." then app becomes sluggish |
| **What Happens** | Writes thousands of verbose log entries per second (Debug/Trace level). Floods stdout and Log Analytics with noise. |
| **Telemetry Signals** | Log Analytics ingestion spike, Prometheus `contosobank_log_entries_total` counter rockets, container I/O metrics |
| **SRE Agent Finds** | Abnormal log volume from single endpoint, log storage costs spiking, I/O saturation |

### Scenario 8: Exception Storm

| Aspect | Detail |
|--------|--------|
| **User Action** | Click "Run Batch Reconciliation" on Reports page |
| **What User Sees** | "Processing batch..." → "Reconciliation failed" |
| **What Happens** | Spawns parallel tasks that throw random exceptions (`NullReferenceException`, `ArgumentException`, `DivideByZeroException`, `FormatException`). Floods App Insights with diverse exception types. |
| **Telemetry Signals** | Exception rate spike in App Insights (multiple exception types), Prometheus `contosobank_exceptions_total` by type |
| **SRE Agent Finds** | Multiple exception types from single operation, stack traces pointing to batch reconciliation code, pattern analysis |

---

## Observability: OpenTelemetry Unified Pipeline

The app uses **OpenTelemetry as a single unified layer** for all telemetry (logs, metrics, traces), with dual export to Azure Monitor and Prometheus. This avoids duplicate metric definitions and aligns with Microsoft's recommended .NET 8 approach.

### NuGet Packages

```xml
<!-- OpenTelemetry Core + Azure Monitor -->
<PackageReference Include="Azure.Monitor.OpenTelemetry.AspNetCore" />

<!-- Prometheus /metrics endpoint -->
<PackageReference Include="OpenTelemetry.Exporter.Prometheus.AspNetCore" />

<!-- Auto-instrumentation -->
<PackageReference Include="OpenTelemetry.Instrumentation.Runtime" />
<PackageReference Include="OpenTelemetry.Instrumentation.SqlClient" />
```

### Program.cs Wiring

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("contoso-bank"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()       // HTTP request traces
        .AddHttpClientInstrumentation()       // outgoing HTTP calls
        .AddSqlClientInstrumentation()        // EF Core / SQL queries
        .AddSource("ContosoBank"))            // custom spans (chaos triggers, business ops)
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()       // request duration, active requests
        .AddRuntimeInstrumentation()          // GC, thread pool, memory
        .AddProcessInstrumentation()          // CPU seconds, working set
        .AddMeter("ContosoBank")              // custom business + reliability metrics
        .AddPrometheusExporter())             // /metrics endpoint for Grafana
    .UseAzureMonitor();                       // → App Insights + Log Analytics

app.MapPrometheusScrapingEndpoint();          // exposes /metrics
```

### Signal Flow to SRE Agent

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          .NET 8 App (OpenTelemetry SDK)                  │
│                                                                          │
│  ILogger<T> ──────┐    System.Diagnostics.Metrics ──┐   ActivitySource ─┐│
│  (structured logs) │    (custom Meter "ContosoBank") │   (custom spans)  ││
│                    │                                  │                   ││
│                    └──────────┬───────────────────────┘───────────────────┘│
│                               │                                           │
│                        OpenTelemetry SDK                                  │
│                        ┌──────┴──────┐                                    │
│                        ▼              ▼                                    │
│              Azure Monitor      Prometheus                                │
│                Exporter          Exporter                                  │
│                   │              (/metrics)                                │
└───────────────────┼──────────────────┼────────────────────────────────────┘
                    │                  │
                    ▼                  ▼
             ┌──────────┐     Managed Prometheus ───▶ Managed Grafana
             │App       │            (scrapes)          (dashboards)
             │Insights +│                                    │
             │Log       │                             MCP endpoint
             │Analytics │                         /api/azure-mcp
             └────┬─────┘                                    │
                  │                                          │
                  └──────────────┬────────────────────────────┘
                                 ▼
                        ┌────────────────┐
                        │  SRE Agent     │
                        │                │
                        │  Built-in:     │  ← KQL queries against App Insights + Log Analytics
                        │  Grafana MCP:  │  ← PromQL queries against Prometheus via Grafana
                        └────────────────┘
```

### What Each Signal Path Gives SRE Agent

| Signal | Destination | SRE Agent Access | What It Sees |
|--------|------------|-----------------|-------------|
| **Traces** | App Insights | Built-in tools (KQL) | Request traces, dependency calls (SQL, HTTP), exception stack traces with full correlation |
| **Metrics** | Azure Monitor | Built-in tools | Platform metrics + custom `ContosoBank.*` meters |
| **Metrics** | Prometheus → Grafana | Grafana MCP connector (PromQL) | Same custom meters via PromQL + dashboard panel visuals |
| **Logs** | Container stdout → Log Analytics | Built-in tools (KQL) | Structured ILogger entries — chaos triggers, errors, debug flood |
| **Logs** | App Insights (via OTel) | Built-in tools | Log entries correlated with traces for full request context |
| **Exceptions** | App Insights | Built-in tools | Exception types, stack traces, frequency, affected endpoints |

### Custom Metrics (defined once, exported to both Azure Monitor and Prometheus)

Using `System.Diagnostics.Metrics` API (registered with OTel via `AddMeter("ContosoBank")`):

```csharp
public class BankMetrics
{
    private readonly Meter _meter = new("ContosoBank");

    // Business Metrics
    public Counter<long> TransfersTotal { get; }           // labels: status=success|failed
    public Counter<long> TransactionsProcessed { get; }    // labels: type=credit|debit
    public UpDownCounter<int> ActiveSessions { get; }

    // Reliability Metrics
    public Histogram<double> RequestDuration { get; }      // labels: endpoint
    public Counter<long> DbErrors { get; }                 // labels: error_type
    public Counter<long> DependencyTimeouts { get; }       // labels: dependency
    public Counter<long> Exceptions { get; }               // labels: exception_type
    public Counter<long> LogEntries { get; }               // labels: level

    // Resource Metrics (supplement auto-collected process_* and runtime_* metrics)
    public UpDownCounter<long> MemoryAllocatedBytes { get; }
    public UpDownCounter<int> DbConnectionsActive { get; }
}
```

These automatically appear as:
- **Prometheus/Grafana**: `contosobank_transfers_total{status="success"}`, `contosobank_request_duration_seconds_bucket{endpoint="/api/transfers"}`
- **Azure Monitor/App Insights**: `ContosoBank/TransfersTotal`, `ContosoBank/RequestDuration`

Auto-collected metrics (no custom code needed):
- `process_cpu_seconds_total`, `process_working_set_bytes` — from `AddProcessInstrumentation()`
- `dotnet_gc_collections_total`, `dotnet_thread_pool_threads` — from `AddRuntimeInstrumentation()`
- `http_server_request_duration_seconds`, `http_server_active_requests` — from `AddAspNetCoreInstrumentation()`

### Structured Logging Strategy

All logging uses `ILogger<T>` with structured properties. The OTel pipeline routes logs to both App Insights and stdout (→ Container Apps → Log Analytics).

```csharp
// Normal operation — Info level
_logger.LogInformation("Transfer {TransferId} processed: {Amount} from {FromAccount} to {ToAccount}",
    transfer.Id, transfer.Amount, transfer.FromAccountId, transfer.ToAccountId);

// Chaos activation — Warning level (SRE Agent picks up on this)
_logger.LogWarning("ChaosScenario activated: {ScenarioName} duration={Duration}s",
    "MemoryLeak", duration.TotalSeconds);

// Errors from chaos — Error level
_logger.LogError(ex, "Database connection failed: {ErrorCode}", sqlEx.Number);

// Log flooding scenario — Debug/Trace level (normally filtered, enabled by chaos)
_logger.LogDebug("Transaction batch item {Index}: account={AccountId} amount={Amount} hash={Hash}",
    i, accountId, amount, hash);
```

Log levels matter for the demo:
- **Normal operation**: Info and above only
- **Log flooding chaos**: Dynamically switches minimum level to `Debug`, flooding Log Analytics
- **SRE Agent investigation**: Queries KQL for error patterns, finds the `ChaosScenario activated` warning log, correlates with metric spikes

---

## Grafana + Prometheus + SRE Agent Integration

This is the centerpiece of the demo — showing SRE Agent can connect to **any data source**, not just native Azure telemetry.

### How the Pipeline Works

```
Container App ──▶ /metrics endpoint ──▶ Azure Monitor Managed Prometheus
     (OTel Prometheus exporter)                   │
                                                  ▼
                                      Azure Managed Grafana
                                      (Prometheus data source)
                                                  │
                                         Built-in MCP endpoint
                                    https://<grafana>/api/azure-mcp
                                                  │
                                                  ▼
                                          Azure SRE Agent
                                    (MCP Connector → Grafana tools)
```

### Azure Managed Grafana MCP Endpoint

Every Azure Managed Grafana workspace now exposes a built-in MCP endpoint at:
```
https://<grafana-endpoint>/api/azure-mcp
```

This means **no separate MCP server to deploy** — Grafana itself is the MCP server. Through this endpoint, SRE Agent gets tools to:

| MCP Tool | What SRE Agent Can Do |
|----------|----------------------|
| `query_prometheus` | Run PromQL queries against any Prometheus data source |
| `list_dashboards` | Discover available Grafana dashboards |
| `get_dashboard` | Retrieve dashboard JSON with panel definitions |
| `query_datasource` | Query any data source connected to Grafana |
| `list_datasources` | Enumerate all configured data sources |
| `search_annotations` | Find Grafana annotations (correlate with events) |

### SRE Agent Grafana Connector Setup (Manual, post-deployment)

After deploying the app infrastructure, the user configures SRE Agent to connect to Grafana:

1. Go to **sre.azure.com** → **Builder** → **Connectors**
2. Click **+ Add connector** → **MCP Server**
3. Enter the Managed Grafana MCP URL: `https://<grafana-name>.grafana.azure.com/api/azure-mcp`
4. Auth: **Managed Identity** (the SRE Agent identity needs `Grafana Admin` or `Grafana Viewer` RBAC on the Grafana workspace)
5. Select tools → **Select all** (or pick specific ones)
6. Save → Status shows **Connected**

### Demo Value: Why Grafana MCP Matters

During a demo, when SRE Agent investigates an incident, it can:

1. **Query Prometheus metrics directly** — e.g., "What was the memory usage trend for the last hour?"
   → SRE Agent runs: `query_prometheus("process_working_set_bytes{job='contoso-bank'}", range="1h")`
2. **Reference dashboards** — e.g., "Show me the Infrastructure Health dashboard"
   → SRE Agent fetches the dashboard and includes metric visualizations in its report
3. **Cross-correlate** — Combine App Insights traces + Prometheus metrics + Log Analytics logs in a single investigation
4. **Demonstrate extensibility** — "SRE Agent doesn't just work with Azure Monitor — it connects to Grafana, Datadog, Splunk, etc. via MCP"

### Grafana Data Sources (configured by Bicep)

The Managed Grafana instance is deployed with these data sources pre-configured:

| Data Source | Type | What It Provides |
|------------|------|-----------------|
| Azure Monitor Managed Prometheus | Prometheus | App custom metrics (`contosobank_*`), process metrics |
| Azure Monitor | Azure Monitor | Azure platform metrics (CPU, memory, network for Container App) |
| Azure Log Analytics | Logs | KQL queries against container and application logs |

### Pre-Built Dashboards

Deployed as provisioned dashboards via post-provision script:

1. **Contoso Bank Overview**
   - Panels: Request rate (by endpoint), error rate (4xx/5xx), P50/P95/P99 latency, active sessions gauge
   - Data source: Prometheus
   - Key for demo: Shows real-time impact when chaos scenarios are triggered

2. **Infrastructure Health**
   - Panels: CPU usage, memory usage, network I/O, container restart count, thread pool size
   - Data source: Prometheus + Azure Monitor
   - Key for demo: Memory leak and CPU spike scenarios create dramatic visual changes

3. **Database Performance**
   - Panels: Query latency histogram, active connections gauge, error rate by type, connection pool utilization
   - Data source: Prometheus (custom `contosobank_db_*` metrics)
   - Key for demo: DB connection failure scenario shows connections dropping to zero

4. **Business Metrics**
   - Panels: Transfers/min (success vs failed), transaction volume, account balance changes, batch processing status
   - Data source: Prometheus (custom `contosobank_transfers_*` metrics)
   - Key for demo: HTTP 500 and exception storm scenarios show business impact (failed transfers)

### Dashboard JSON Structure

Each dashboard JSON lives in `grafana/dashboards/` and includes:
- Panel definitions with PromQL queries
- Variable templates (for environment/namespace filtering)
- Alert thresholds matching the Azure Monitor alert rules
- Time range defaults optimized for demo visibility (last 15 minutes)

---

## Infrastructure as Code (Bicep + azd)

### Deployed Resources

| Resource | Bicep Module | Purpose |
|----------|-------------|---------|
| Resource Group | `main.bicep` | Contains all resources |
| Container Apps Environment | `modules/container-env.bicep` | Hosting environment |
| Container App | `modules/container-app.bicep` | The Contoso Bank app |
| Azure SQL Server + DB | `modules/sql.bicep` | Application database |
| Application Insights | `modules/monitoring.bicep` | APM + telemetry |
| Log Analytics Workspace | `modules/monitoring.bicep` | Log aggregation |
| Azure Monitor Workspace | `modules/prometheus.bicep` | Managed Prometheus + scrape config for Container App |
| Azure Managed Grafana | `modules/grafana.bicep` | Dashboards + MCP endpoint for SRE Agent |
| Data Collection Rule | `modules/prometheus.bicep` | Routes Prometheus metrics from Container App → Monitor Workspace |
| Alert Rules | `modules/alerts.bicep` | CPU, memory, HTTP 5xx, latency |
| Managed Identity | `modules/identity.bicep` | RBAC for app + monitoring + Grafana Admin |

### One-Command Deployment

```bash
azd up    # Deploys everything
azd down  # Tears it all down
```

### Post-Provisioning Script

Automated by `scripts/post-provision.sh`:
- Seeds the database with sample accounts and transactions
- Configures Grafana data sources (Prometheus, Azure Monitor, Log Analytics)
- Imports Grafana dashboard JSON definitions
- Configures Prometheus scraping for the Container App `/metrics` endpoint
- Outputs the Grafana MCP endpoint URL for SRE Agent connector setup
- Assigns `Grafana Admin` role to the deployment identity

---

## ChaosService Design

The `ChaosService` is registered as a singleton in DI. It provides methods that controllers call to trigger specific failures. Each chaos method:

1. **Activates the failure** (e.g., starts memory allocation, sets error flag)
2. **Auto-recovers after a configurable duration** (default: 5 minutes) — so the demo can be repeated
3. **Records activation in logs** with structured logging (so SRE Agent can find root cause in code)
4. **Increments Prometheus counters** for each chaos type activated

```csharp
public interface IChaosService
{
    Task TriggerMemoryLeak(CancellationToken ct);
    Task TriggerCpuSpike(CancellationToken ct);
    void TriggerHttpErrors(TimeSpan duration);
    void TriggerDbConnectionFailure(TimeSpan duration);
    void TriggerSlowResponses(TimeSpan duration);
    Task TriggerDependencyTimeout(CancellationToken ct);
    Task TriggerLogFlooding(CancellationToken ct);
    Task TriggerExceptionStorm(CancellationToken ct);
    ChaosStatus GetStatus();  // Returns which failures are currently active
}
```

---

## SRE Agent Integration Points

The app itself does NOT deploy or configure SRE Agent. However, it is designed to be a perfect investigation target:

1. **Application Insights** — Traces, exceptions, dependencies, and request telemetry automatically collected
2. **Log Analytics** — All container logs flow here; SRE Agent queries via KQL
3. **Azure Monitor Alerts** — Pre-configured alert rules fire on CPU > 90%, memory > 85%, HTTP 5xx > 10/min, P99 latency > 10s
4. **Prometheus /metrics** — SRE Agent connects via Grafana MCP connector to query custom metrics
5. **Grafana Dashboards** — SRE Agent can reference dashboards during investigation and include chart screenshots in reports
6. **Source Code (GitHub)** — SRE Agent can search the repo for root cause analysis, finding `ChaosService` as the culprit

---

## Demo Flow

1. **Open Contoso Bank** in browser — show it's a working banking app
2. **Open Grafana** side-by-side — show healthy metrics/dashboards
3. **Open SRE Agent portal** (sre.azure.com) — show it's monitoring the resources
4. **Trigger a scenario** — click a button in the banking app (e.g., "Generate Annual Statement")
5. **Watch Grafana** — see metrics change in real-time (memory climbing)
6. **Watch SRE Agent** — see it detect the alert, investigate, correlate logs/metrics, identify root cause
7. **Show resolution** — SRE Agent proposes mitigation (restart container, scale out, etc.)
8. **Repeat** with different scenario types to show breadth of SRE Agent capabilities

---

## Project Structure

```
contoso-bank/
├── azure.yaml                          # azd project definition
├── SPEC.md                             # This file — full spec + plan
├── README.md                           # User-facing deployment + demo guide
├── infra/
│   ├── main.bicep                      # Subscription-scoped entry point
│   ├── main.bicepparam                 # Parameter defaults
│   └── modules/
│       ├── container-env.bicep         # Container Apps Environment
│       ├── container-app.bicep         # The Contoso Bank Container App
│       ├── sql.bicep                   # Azure SQL Server + Database
│       ├── monitoring.bicep            # App Insights + Log Analytics workspace
│       ├── prometheus.bicep            # Azure Monitor Workspace + Data Collection Rule
│       ├── grafana.bicep               # Azure Managed Grafana + data sources
│       ├── alerts.bicep                # Azure Monitor alert rules
│       └── identity.bicep              # Managed Identity + RBAC assignments
├── src/
│   └── ContosoBank/
│       ├── ContosoBank.csproj          # .NET 8 project with OTel + EF Core packages
│       ├── Program.cs                  # App startup, DI, OTel pipeline, middleware
│       ├── appsettings.json            # Config (connection strings, feature flags)
│       ├── appsettings.Development.json
│       ├── Dockerfile                  # Multi-stage build for Container Apps
│       ├── Models/
│       │   ├── Account.cs
│       │   ├── Transaction.cs
│       │   └── Transfer.cs
│       ├── Data/
│       │   ├── BankDbContext.cs         # EF Core DbContext
│       │   └── SeedData.cs             # Initial data seeding logic
│       ├── Services/
│       │   ├── IChaosService.cs        # Chaos interface
│       │   ├── ChaosService.cs         # Failure injection engine (singleton)
│       │   ├── AccountService.cs       # Account CRUD operations
│       │   ├── TransferService.cs      # Transfer processing logic
│       │   ├── TransactionService.cs   # Transaction queries
│       │   └── ReportService.cs        # Report generation
│       ├── Controllers/
│       │   ├── AccountsController.cs   # GET /api/accounts, GET /api/accounts/{id}
│       │   ├── TransfersController.cs  # POST /api/transfers, POST /api/transfers/wire
│       │   ├── TransactionsController.cs # GET /api/transactions
│       │   ├── ReportsController.cs    # POST /api/reports/annual-statement, POST /api/reports/reconciliation
│       │   ├── SettingsController.cs   # POST /api/settings/verify-identity
│       │   └── HealthController.cs     # GET /health, GET /health/ready
│       ├── Pages/
│       │   ├── _Layout.cshtml          # Banking UI shell (sidebar nav, header, footer)
│       │   ├── _Layout.cshtml.cs
│       │   ├── Index.cshtml            # Dashboard — account overview + "Run Fraud Detection" button
│       │   ├── Index.cshtml.cs
│       │   ├── Accounts.cshtml         # Account list — "Refresh" balance button
│       │   ├── Accounts.cshtml.cs
│       │   ├── Transfers.cshtml        # Transfer form — "Wire Transfer" + "International Transfer" buttons
│       │   ├── Transfers.cshtml.cs
│       │   ├── Transactions.cshtml     # Transaction history — "Export Full History" button
│       │   ├── Transactions.cshtml.cs
│       │   ├── Reports.cshtml          # Reports — "Annual Statement" + "Batch Reconciliation" buttons
│       │   ├── Reports.cshtml.cs
│       │   ├── Settings.cshtml         # Settings — "Verify Identity" button
│       │   └── Settings.cshtml.cs
│       ├── wwwroot/
│       │   ├── css/
│       │   │   └── site.css            # Banking theme CSS (professional, dark blue/white)
│       │   ├── js/
│       │   │   └── site.js             # AJAX calls, loading spinners, toast notifications
│       │   ├── favicon.ico
│       │   └── images/
│       │       └── logo.svg            # Contoso Bank logo
│       └── Metrics/
│           └── BankMetrics.cs          # Custom System.Diagnostics.Metrics definitions
├── tests/
│   └── ContosoBank.Tests/
│       ├── ContosoBank.Tests.csproj
│       ├── Controllers/                # API controller unit tests
│       ├── Services/                   # Service layer unit tests
│       └── Integration/               # Integration tests (WebApplicationFactory)
├── scripts/
│   ├── post-provision.sh               # azd hook: DB seed + Grafana dashboards + Prometheus config
│   └── seed-data.sql                   # Sample banking data (accounts, transactions, transfers)
├── grafana/
│   └── dashboards/
│       ├── overview.json               # Contoso Bank Overview dashboard
│       ├── infrastructure.json         # Infrastructure Health dashboard
│       ├── database.json               # Database Performance dashboard
│       └── business.json               # Business Metrics dashboard
└── .github/
    └── workflows/
        └── ci.yml                      # Build + test CI pipeline
```

---

## Skills and Tools for Implementation

During implementation, leverage the following Copilot skills and tools for maximum efficiency:

### Installed Copilot Skills (from skills-lock.json)

| Skill | When to Use |
|-------|------------|
| **appinsights-instrumentation** | When wiring up Application Insights and OpenTelemetry configuration in Program.cs |
| **azure-deploy** | When deploying the app to Azure Container Apps |
| **azure-prepare** | When setting up the Azure environment and prerequisites |
| **azure-rbac** | When configuring RBAC for Managed Identity (Grafana Admin, Monitoring Reader, etc.) |
| **azure-resource-lookup** | When looking up deployed resource IDs, connection strings, endpoints |
| **azure-storage** | If any blob storage integration is needed |
| **azure-validate** | When validating Azure resource configurations and Bicep templates |
| **directory-build-organization** | When organizing the .NET solution structure and Directory.Build.props |
| **entra-app-registration** | If Entra ID authentication is needed for the banking app |
| **msbuild-antipatterns** | When reviewing the .csproj for build issues |
| **run-tests** | When running .NET unit/integration tests |

### Available Agent Skills (from session)

| Skill | When to Use |
|-------|------------|
| **speckit.specify** | If the spec needs further refinement or clarification |
| **speckit.plan** | To generate a detailed implementation plan from this spec |
| **speckit.tasks** | To generate dependency-ordered tasks from the plan |
| **speckit.implement** | To execute implementation tasks |
| **speckit.analyze** | To cross-check spec, plan, and tasks for consistency |
| **azd-app** | When running the app locally, managing services, checking health |
| **find-skills** | To discover additional skills needed during implementation |

### External Tools and Skills to Invoke

| Tool/Skill | When to Use | How to Invoke |
|-----------|------------|---------------|
| **playwright-cli** | UI testing and validation — verify Razor Pages render correctly, test chaos buttons, validate dashboard layout, check responsive design | Use the `playwright-cli` skill for browser automation, screenshots, and E2E testing |
| **dotnet CLI** | Scaffolding projects, adding packages, running builds/tests | `dotnet new`, `dotnet add package`, `dotnet build`, `dotnet test` |
| **azd CLI** | Deployment and provisioning | `azd init`, `azd up`, `azd down`, `azd env set` |
| **az CLI** | Azure resource management outside Bicep | `az sql`, `az grafana`, `az monitor` |

### Playwright UI Testing Strategy

Use the `playwright-cli` skill for end-to-end validation:

1. **Page Load Tests** — Verify each Razor Page loads without errors (Dashboard, Accounts, Transfers, Transactions, Reports, Settings)
2. **Navigation Tests** — Verify sidebar navigation works, active page highlighting
3. **Chaos Button Tests** — Click each chaos-trigger button, verify:
   - The UI shows appropriate loading/error state
   - The correct API endpoint is called
   - Error messages display gracefully (no raw stack traces)
4. **Responsive Design** — Verify layout works on desktop and tablet viewports
5. **Accessibility** — Basic a11y checks (heading hierarchy, form labels, color contrast)
6. **Visual Regression** — Screenshot comparisons for the banking theme consistency

Playwright is configured in `.playwright/cli.config.json` to use Chromium/Edge.

---

## Implementation Plan — Dependency-Ordered Tasks

### Phase 1: Foundation (no dependencies)

| # | Task | Description |
|---|------|-------------|
| 1 | **Scaffold .NET 8 project** | `dotnet new webapp` in `src/ContosoBank/`. Add NuGet packages: `Azure.Monitor.OpenTelemetry.AspNetCore`, `OpenTelemetry.Exporter.Prometheus.AspNetCore`, `OpenTelemetry.Instrumentation.Runtime`, `OpenTelemetry.Instrumentation.SqlClient`, `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`. Configure `ContosoBank.csproj`. |
| 2 | **Create data models + DbContext** | `Account.cs`, `Transaction.cs`, `Transfer.cs` in `Models/`. `BankDbContext.cs` with EF Core configuration, indexes, and relationships. `SeedData.cs` for initial demo data. |

### Phase 2: Core Application (depends on Phase 1)

| # | Task | Description |
|---|------|-------------|
| 3 | **Build Razor Pages banking UI** | Professional banking theme with sidebar nav. Pages: Dashboard (`Index.cshtml`), Accounts, Transfers, Transactions, Reports, Settings. Dark blue/white color scheme. Each page includes the natural-looking buttons that will trigger chaos scenarios. Mobile-friendly layout. |
| 4 | **Implement API controllers** | `AccountsController`, `TransfersController`, `TransactionsController`, `ReportsController`, `SettingsController`, `HealthController`. Full CRUD operations against EF Core. Proper HTTP status codes and error handling. |
| 5 | **Implement service layer** | `AccountService`, `TransferService`, `TransactionService`, `ReportService`. Business logic separated from controllers. Inject `ILogger<T>` and `BankMetrics` for structured logging and metrics. |

### Phase 3: Observability (depends on Phase 1)

| # | Task | Description |
|---|------|-------------|
| 6 | **Configure OpenTelemetry pipeline** | Wire up `AddOpenTelemetry()` in `Program.cs` with tracing (ASP.NET Core, HttpClient, SqlClient, custom source), metrics (ASP.NET Core, Runtime, Process, custom meter, Prometheus exporter), and `UseAzureMonitor()`. Map `/metrics` endpoint. |
| 7 | **Create BankMetrics class** | `Metrics/BankMetrics.cs` using `System.Diagnostics.Metrics`. Register as singleton in DI. Instrument all service methods to record business and reliability metrics. |

### Phase 4: Chaos Engineering (depends on Phase 2 + Phase 3)

| # | Task | Description |
|---|------|-------------|
| 8 | **Build ChaosService** | `Services/ChaosService.cs` — singleton implementing all 8 failure scenarios. Each method: activates failure, auto-recovers after configurable duration, logs activation at Warning level, increments chaos-specific metrics. Thread-safe with `ConcurrentDictionary` for active scenario tracking. |
| 9 | **Wire chaos into controllers** | Map each chaos trigger to a natural controller action. The controller methods look like normal banking operations but call `IChaosService` methods. Ensure graceful error handling — user sees a realistic banking error, not a raw stack trace. |

### Phase 5: Containerization (depends on Phase 2)

| # | Task | Description |
|---|------|-------------|
| 10 | **Create Dockerfile** | Multi-stage Dockerfile: `mcr.microsoft.com/dotnet/sdk:8.0` for build, `mcr.microsoft.com/dotnet/aspnet:8.0` for runtime. Expose port 8080. Set `ASPNETCORE_URLS`. Health check instruction. Optimize layer caching (copy `.csproj` first, then `dotnet restore`, then copy source). |

### Phase 6: Infrastructure (depends on Phase 5)

| # | Task | Description |
|---|------|-------------|
| 11 | **Write Bicep IaC modules** | All modules in `infra/modules/`: `container-env.bicep`, `container-app.bicep`, `sql.bicep`, `monitoring.bicep`, `prometheus.bicep`, `grafana.bicep`, `alerts.bicep`, `identity.bicep`. Entry point `main.bicep` orchestrating all modules. `main.bicepparam` with sensible defaults. |
| 12 | **Create azd configuration** | `azure.yaml` defining the project, services, and hooks. Post-provision hook pointing to `scripts/post-provision.sh`. Environment variable mapping for connection strings and instrumentation keys. |

### Phase 7: Dashboards + Automation (depends on Phase 6)

| # | Task | Description |
|---|------|-------------|
| 13 | **Build Grafana dashboard JSONs** | 4 dashboards in `grafana/dashboards/`: Overview, Infrastructure, Database, Business. Each with PromQL queries targeting `contosobank_*` and `process_*` metrics. Variable templates for environment filtering. 15-minute default time range. |
| 14 | **Write database seed script** | `scripts/seed-data.sql` — realistic banking data: 5 accounts (2 checking, 1 savings, 1 credit, 1 business), 100+ transactions across 30 days, 10+ recent transfers. |
| 15 | **Write post-provision script** | `scripts/post-provision.sh` — runs after `azd provision`: executes SQL seed script, configures Grafana data sources via API, imports dashboard JSONs, outputs Grafana MCP URL. |

### Phase 8: Testing + Documentation (depends on all above)

| # | Task | Description |
|---|------|-------------|
| 16 | **UI testing with Playwright** | Use the `playwright-cli` skill to validate: all pages load, navigation works, chaos buttons trigger correctly, error states display gracefully, responsive layout, basic accessibility. |
| 17 | **Write unit + integration tests** | Controller tests (mock services), service tests (mock DbContext), integration tests with `WebApplicationFactory`. Verify chaos scenarios activate/deactivate correctly. Use `run-tests` skill. |
| 18 | **Write README** | User-facing documentation: prerequisites, one-command deployment, SRE Agent connector setup guide, demo walkthrough for each scenario, troubleshooting, cleanup. |

---

## Design Decisions and Rationale

| Decision | Rationale |
|----------|-----------|
| **ASP.NET Core Razor Pages** (not SPA) | Simpler deployment (single container), no CORS issues, SSR for fast page loads during demo |
| **OpenTelemetry** (not App Insights SDK + prometheus-net) | Single instrumentation API, dual export, Microsoft's recommended path, avoids duplicate metric definitions |
| **Azure Container Apps** (not App Service/AKS) | SRE Agent has deep Container Apps diagnostics, simpler than AKS, cheaper than App Service for demo |
| **Azure SQL** (not Cosmos DB) | Familiar relational model for banking, EF Core first-class support, easy to demonstrate connection failure chaos |
| **Managed Grafana** (not self-hosted) | Built-in MCP endpoint, no infra to manage, Azure RBAC integration, deployed via Bicep |
| **Singleton ChaosService** (not middleware/feature flags) | Fine-grained control per scenario, auto-recovery timers, status tracking, doesn't pollute middleware pipeline |
| **5-minute auto-recovery** | Demo can be repeated without manual cleanup; long enough for SRE Agent to detect and investigate |

---

## Appendix A: Azure SRE Agent Background

Azure SRE Agent is an AI-driven platform that automates site reliability engineering. Key capabilities relevant to this demo:

- **Automated investigation**: Queries App Insights, Log Analytics, Azure Monitor metrics, and external data sources (via MCP) to diagnose issues
- **Hypothesis-driven root cause analysis**: Tests multiple potential causes, correlates evidence across signals
- **Automated mitigation**: Can restart services, scale resources, apply config fixes — with human approval in the loop
- **MCP connector extensibility**: Connects to Grafana, Datadog, Splunk, GitHub, and any MCP-compatible server
- **Deep investigation mode**: For complex multi-signal incidents, runs extended analysis with multiple hypotheses
- **Knowledge building**: Remembers past incidents and resolutions, gets smarter over time
- **Incident platforms**: Integrates with PagerDuty, ServiceNow, Azure Monitor Alerts

For more info: https://learn.microsoft.com/en-us/azure/sre-agent/overview

## Appendix B: Reference Samples

- **Grubify** (Microsoft hands-on lab): https://github.com/microsoft/sre-agent/tree/main/samples/hands-on-lab
- **Octopets** (automation sample): https://github.com/microsoft/sre-agent/tree/main/samples/automation
- **Grafana MCP docs**: https://learn.microsoft.com/en-us/azure/managed-grafana/grafana-mcp-server
- **SRE Agent MCP connector tutorial**: https://learn.microsoft.com/en-us/azure/sre-agent/mcp-connector
- **OpenTelemetry .NET + Prometheus + Grafana example**: https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-prgrja-example
