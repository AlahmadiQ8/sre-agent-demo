# Contoso Bank — Azure SRE Agent Demo

A demo banking application for [Azure SRE Agent](https://learn.microsoft.com/en-us/azure/sre-agent/overview). Failure scenarios are embedded into normal banking workflows — clicking buttons like "Generate Annual Statement" or "Run Fraud Detection" silently triggers realistic production issues (memory leaks, CPU spikes, HTTP 500s, etc.) that SRE Agent can detect, investigate, and mitigate.

## Architecture

```mermaid
graph TD
    subgraph ACA["Azure Container Apps"]
        subgraph WebApp["Contoso Bank Web App"]
            Razor["Razor Pages<br/>(Frontend)"] -->|requests| API["ASP.NET Core Web API<br/>Controllers + Services<br/>ChaosService (DI)"]
        end
    end

    API -->|"UseAzureMonitor()"| AppInsights["App Insights +<br/>Log Analytics"]
    API --> SQL[("Azure SQL<br/>Database")]
    AppInsights -->|"KQL queries"| Grafana["Managed Grafana<br/>(dashboard + MCP)"]

    AppInsights --> SRE["Azure SRE Agent<br/>(configured separately)"]
    Grafana --> SRE
```

### Tech Stack

| Layer | Technology | Azure Service |
|-------|-----------|---------------|
| Frontend | ASP.NET Core Razor Pages | — |
| Backend | ASP.NET Core Web API (C# / .NET 10) | Azure Container Apps |
| Database | Entity Framework Core | Azure SQL Database |
| Observability | OpenTelemetry SDK (logs, metrics, traces) | App Insights + Log Analytics |
| Dashboards | Azure Managed Grafana (KQL) | Grafana + MCP endpoint |
| IaC | Bicep + Azure Developer CLI (`azd`) | One-command deployment |

### Deployed Resources

| Resource | Bicep Module | Purpose |
|----------|-------------|---------|
| Resource Group | `main.bicep` | Contains all resources |
| Container Apps Environment + ACR | `modules/container-env.bicep` | App hosting + image registry |
| Container App | `modules/container-app.bicep` | The Contoso Bank app |
| Azure SQL Server + DB | `modules/sql.bicep` | Application database (AAD-only auth) |
| Application Insights + Log Analytics | `modules/monitoring.bicep` | APM, telemetry, log aggregation |
| Azure Monitor Workspace | `modules/monitor-workspace.bicep` | Metrics backend for Grafana |
| Azure Managed Grafana | `modules/grafana.bicep` | Dashboards (KQL) + MCP endpoint for SRE Agent |
| Alert Rules + Action Group | `modules/alerts.bicep` | 10 pre-configured alerts mapped to chaos scenarios |
| Managed Identity | `modules/identity.bicep` | RBAC for app, monitoring, and Grafana |

## Prerequisites

- [Azure Developer CLI (`azd`)](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd) v1.9+
- [Azure CLI (`az`)](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) v2.60+
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for local container builds)
- An Azure subscription with permissions to create resources (Contributor + User Access Administrator)

## Quick Start

### One-Command Deployment

```bash
# Clone the repo
git clone https://github.com/<org>/sre-agent-demo.git
cd sre-agent-demo

# Deploy everything (infra + app + monitoring + dashboards)
azd up
```

`azd up` provisions all Azure resources, builds and deploys the container, and runs the post-provision script which:

1. Grants managed identity access to SQL Database
2. Seeds the database with demo banking data
3. Configures Container Apps OpenTelemetry agent
4. Sets up Grafana data sources (Azure Monitor, Log Analytics)
5. Imports the Grafana dashboard
6. Outputs the Grafana MCP endpoint URL and SRE Agent setup instructions

### Local Development

```bash
# Build and run locally (uses EF Core InMemory database)
cd src/ContosoBank
dotnet run

# Or run with Docker
docker build -t contoso-bank .
docker run -p 8080:8080 contoso-bank
```

The app is available at `http://localhost:8080` with these endpoints:

| Endpoint | Purpose |
|----------|---------|
| `/` | Banking dashboard (home page) |
| `/health/live` | Liveness probe (always 200) |
| `/health/ready` | Readiness probe (checks DB connectivity) |

### Local Grafana

The `grafana/` directory contains a Docker Compose setup that runs Grafana locally, connected directly to your Azure Monitor / Log Analytics workspace via managed identity — no Prometheus needed.

```bash
cd grafana
docker compose up -d
# Grafana is available at http://localhost:3000 (admin/admin)
```

The local Grafana instance is pre-provisioned with:
- **Azure Monitor datasource** — authenticates via managed identity (`azureAuthType: msi`)
- **Contoso Bank dashboard** — the same KQL-based dashboard deployed to Azure Managed Grafana

## Chaos Scenarios

The demo includes **8 distinct failure scenarios**, each triggered by a natural-looking banking action. Every scenario auto-recovers after 5 minutes so the demo can be repeated without manual cleanup.

| # | Scenario | Page → Button | What Breaks |
|---|----------|--------------|-------------|
| 1 | **Memory Leak** | Reports → "Generate Annual Statement" | Allocates large byte arrays, causes OOM kill |
| 2 | **CPU Spike** | Dashboard → "Run Fraud Detection" | CPU-intensive hash computation, pins all cores |
| 3 | **HTTP 500 Errors** | Transfers → "Wire Transfer" | `InvalidOperationException` on every request |
| 4 | **DB Connection Failure** | Accounts → "Refresh" | `DbConnectionInterceptor` blocks all DB queries |
| 5 | **Slow API (30s)** | Transfers → "International Transfer" | `Task.Delay(30s)` in transfer pipeline |
| 6 | **Dependency Timeout** | Settings → "Verify Identity (KYC)" | HTTP call to non-routable IP, thread pool exhaustion |
| 7 | **Log Flooding** | Transactions → "Export Full History" | Thousands of verbose log entries per second |
| 8 | **Exception Storm** | Reports → "Run Batch Reconciliation" | Parallel tasks throwing diverse exception types |

> 📋 See [DEMO.md](DEMO.md) for a detailed demo walkthrough with suggested flow and best starting scenarios.

### Observability Signals

Each chaos scenario produces distinct telemetry that SRE Agent can investigate:

```mermaid
graph TD
    subgraph DotNetApp[".NET 10 App (OpenTelemetry SDK)"]
        Logs["ILogger&lt;T&gt;<br/>(structured logs)"] --> OTel["OpenTelemetry SDK"]
        Metrics["System.Diagnostics.Metrics<br/>(custom 'ContosoBank' meter)"] --> OTel
        Traces["ActivitySource<br/>(custom spans)"] --> OTel

        OTel --> AzExporter["Azure Monitor<br/>Exporter"]
    end

    AzExporter --> AppInsights["App Insights +<br/>Log Analytics"]
    AppInsights -->|"KQL queries"| Grafana["Managed Grafana<br/>(dashboard)"]
    Grafana --> MCP["MCP endpoint"]

    AppInsights -->|"KQL queries"| SRE["SRE Agent"]
    MCP -->|"KQL queries"| SRE
```

## Azure SRE Agent Setup

### Grafana MCP Connector

Every Azure Managed Grafana instance exposes a built-in MCP endpoint at `/api/azure-mcp` ([docs](https://github.com/Azure/azure-managed-grafana/blob/main/amg-mcp.md)). After `azd up`, find your endpoint:

```bash
# Get the Grafana MCP endpoint from azd environment
azd env get-value GRAFANA_MCP_ENDPOINT
# Example: https://graf-<name>.cse.grafana.azure.com/api/azure-mcp
```

#### Authentication: Grafana Service Account Token (Recommended)

Use a Grafana service account token for a **one-time setup** that doesn't expire:

1. Open your Grafana instance (get the URL with `azd env get-value GRAFANA_ENDPOINT`)
2. Navigate to **Administration → Service accounts**
3. Create a new service account with **Viewer** role
4. Click **Add token** → generate a token (format: `glsa_xxx`)
5. Copy the token — you'll use it in the connector config below

> **Alternative:** Use an Entra ID token for short-lived access (expires in ~1 hour):
> ```bash
> az account get-access-token --resource ce34e7e5-485f-4d76-964f-b3d2b16d1e4f --query accessToken -o tsv
> ```

#### Connect SRE Agent

1. Go to [sre.azure.com](https://sre.azure.com) → **Builder** → **Connectors**
2. Click **+ Add connector** → **MCP Server**
3. Enter the MCP URL from `azd env get-value GRAFANA_MCP_ENDPOINT`
4. Auth: **Bearer token** — paste the Grafana service account token (`glsa_xxx`)
5. Select tools → **Select all**
6. Save — status should show **Connected**

Through this MCP endpoint, SRE Agent gets access to tools like `amgmcp_query_resource_log`, `amgmcp_insights_get_failures`, `amgmcp_prometheus_query`, and more — enabling it to query App Insights, Log Analytics, and Azure Monitor metrics directly via Grafana.

### Investigation Workflows

<!-- TODO: Document SRE Agent investigation workflows for each chaos scenario -->

### Mitigation Playbooks

<!-- TODO: Document automated mitigation actions SRE Agent can take -->

## Project Structure

```
├── azure.yaml                    # azd project manifest
├── DEMO.md                       # Demo cheat sheet & walkthrough
├── ContosoBank.slnx              # Solution file
├── grafana/
│   ├── docker-compose.yml        # Local Grafana (Azure Monitor via managed identity)
│   ├── dashboards/
│   │   └── contoso-bank.json     # Grafana dashboard (KQL panels)
│   └── provisioning/
│       ├── dashboards/
│       │   └── default.yaml      # Dashboard provisioning config
│       └── datasources/
│           └── azure-monitor.yaml # Azure Monitor datasource (MSI auth)
├── infra/
│   ├── main.bicep                # Main orchestration (subscription-scoped)
│   ├── main.bicepparam           # Parameter defaults
│   └── modules/                  # 8 Bicep modules (see Deployed Resources)
├── scripts/
│   ├── post-provision.sh         # Post-azd setup (SQL, Grafana, OTel)
│   ├── seed-data.sql             # Demo banking data (5 accounts, 112 txns)
│   └── validate-dashboard.sh     # Dashboard JSON validation
├── src/
│   └── ContosoBank/
│       ├── Controllers/          # API controllers (5)
│       ├── Data/                 # EF Core DbContext + seed data
│       ├── Interceptors/         # ChaosDbInterceptor (DB failure injection)
│       ├── Metrics/              # BankMetrics (custom OTel meters)
│       ├── Models/               # Account, Transaction, Transfer
│       ├── Pages/                # Razor Pages (Dashboard, Accounts, etc.)
│       ├── Services/             # Business logic + ChaosService
│       ├── Dockerfile            # Multi-stage .NET 10 build
│       └── Program.cs            # App config (OTel, health checks, DI)
├── tests/
│   └── ContosoBank.Tests/        # Unit + integration tests
└── specs/
    └── 01-spec.md                # Full specification
```

## Running Tests

```bash
dotnet test
```

Tests use `WebApplicationFactory` with EF Core InMemory provider. No external dependencies required.

## Cleanup

```bash
# Remove all Azure resources
azd down

# Remove with force (skip confirmation)
azd down --force --purge
```

This deletes the resource group and all resources within it, including the SQL database, Container App, Grafana instance, and all monitoring data.

## Troubleshooting

| Issue | Solution |
|-------|----------|
| `azd up` fails on SQL firewall | Your IP may have changed. Re-run `azd up` — the post-provision script adds a temporary firewall rule. |
| Grafana shows "No data" | Wait 2–3 minutes after deployment for telemetry to flow. Verify App Insights connection string is set on the Container App. |
| Container restarts in a loop | Check container logs: `az containerapp logs show --name <app> --resource-group <rg>`. Common cause: missing SQL connection string. |
| Health check fails (`/health/ready`) | Database connectivity issue. Verify SQL Server firewall rules and managed identity access. |
| Chaos scenario doesn't auto-recover | Each scenario has a 5-minute timer. Wait for auto-recovery or restart the Container App. |
| `sqlcmd` not found during post-provision | Install [sqlcmd](https://learn.microsoft.com/en-us/sql/tools/sqlcmd/sqlcmd-utility): `brew install sqlcmd` (macOS) or `apt-get install mssql-tools` (Linux). |

## License

This project is for demonstration purposes. See [LICENSE](LICENSE) for details.
