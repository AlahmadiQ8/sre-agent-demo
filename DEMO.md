# Contoso Bank — Chaos Demo Cheat Sheet

Quick reference for triggering failure scenarios during live demos.

## How It Works

Every chaos scenario is triggered by a **normal-looking banking button** — there's no separate chaos panel or CLI. The audience sees a realistic app breaking naturally, then you switch to Grafana / SRE Agent to watch it investigate.

All scenarios **auto-recover after 5 minutes**, so you can repeat the demo.

## Chaos Trigger Map

| # | Page | Button to Click | Failure Triggered | What Audience Sees | What SRE Agent Finds |
|---|------|----------------|-------------------|--------------------|-----------------------|
| 1 | **Reports** | Generate Annual Statement | Memory Leak | Loading → timeout/error | Memory growth, OOM kill, `dotnet.process.memory.working_set` spike |
| 2 | **Dashboard** | Run Fraud Detection | CPU Spike | "Scanning transactions..." | CPU > 95%, thread starvation, `process.cpu.time` spike |
| 3 | **Transfers** | Wire Transfer | HTTP 500 Errors | "Transfer failed" error | 5xx spike on `/api/transfers/wire`, exception traces |
| 4 | **Accounts** | Refresh | DB Connection Failure | "Unable to load account information" | `SqlException` flood, `contosobank.db.errors` spike |
| 5 | **Transfers** | International Transfer | Slow API (30s delay) | Very slow loading | P99 latency spike, no CPU/memory issue → external dependency |
| 6 | **Settings** | Verify Identity (KYC) | Dependency Timeout | "Identity verification failed" | Timeout exceptions, `contosobank.dependency.timeouts` spike |
| 7 | **Transactions** | Export Full History | Log Flooding | "Preparing export..." → sluggish app | Log volume spike, `contosobank.log.entries` rockets |
| 8 | **Reports** | Run Batch Reconciliation | Exception Storm | "Reconciliation failed" | Multiple exception types, `contosobank.exceptions` spike |

## Suggested Demo Flow

1. **Open the app** in browser — show it's a working banking app
2. **Open Grafana** side-by-side — show healthy dashboard
3. **Open SRE Agent** (sre.azure.com) — show it's monitoring
4. **Click a button** (e.g., "Generate Annual Statement" on Reports)
5. **Watch Grafana** — see metrics change in real-time
6. **Watch SRE Agent** — see it detect, investigate, and correlate
7. **Repeat** with a different scenario to show breadth

## Best Scenarios to Start With

- **Memory Leak** (Reports → Annual Statement) — dramatic, visual in Grafana
- **HTTP 500** (Transfers → Wire Transfer) — fast, immediate error spike
- **Exception Storm** (Reports → Batch Reconciliation) — shows diverse exception analysis

---

## Traffic Simulation with k6

Instead of manually clicking chaos buttons, use **k6 scripts** to automate traffic generation and reliably fire Azure Monitor alerts within 1–2 minutes.

### Prerequisites

```bash
brew install k6    # macOS
# or: go install go.k6.io/k6@latest
```

### Quick Start

```bash
# Start baseline traffic (run before demo, keep in background)
./tests/k6/run-scenario.sh baseline --base-url https://your-app.azurecontainerapps.io

# Trigger a single chaos scenario
./tests/k6/run-scenario.sh memory-leak --base-url https://your-app.azurecontainerapps.io

# Trigger all chaos scenarios sequentially
./tests/k6/run-scenario.sh all --base-url https://your-app.azurecontainerapps.io

# Cherry-pick specific scenarios
./tests/k6/run-scenario.sh all --only http-500,db-failure --base-url https://your-app.azurecontainerapps.io

# Run all except slow ones
./tests/k6/run-scenario.sh all --disable memory-leak,slow-api --base-url https://your-app.azurecontainerapps.io

# Preview what would run
./tests/k6/run-scenario.sh all --only http-500,slow-api --dry-run

# List available scenarios
./tests/k6/run-scenario.sh --list
```

### Alert-to-Scenario Mapping

| Scenario | Alerts Triggered | Chaos Endpoint |
|----------|-----------------|----------------|
| `memory-leak` | A1 (High Memory), A2 (OOM Restart) | `POST /api/reports/annual-statement` |
| `cpu-spike` | A3 (High CPU) | `POST /api/accounts/fraud-detection` |
| `http-500` | A4 (HTTP 5xx) | `POST /api/transfers/wire` |
| `db-failure` | A5 (DB Failures), A10 (Health Degraded) | `POST /api/accounts/refresh` |
| `slow-api` | A6 (P95 Latency) | `POST /api/transfers/international` |
| `dependency-timeout` | A7 (Dependency Timeout) | `POST /api/settings/verify-identity` |
| `log-flood` | A8 (Log Volume) | `POST /api/transactions/export` |
| `exception-storm` | A9 (Exception Storm) | `POST /api/reports/reconciliation` |

### Recommended Demo Flow with k6

1. **Start baseline traffic** — `./tests/k6/run-scenario.sh baseline --base-url <url>` (keep running in a background terminal)
2. **Open Grafana** side-by-side — show healthy dashboard with live traffic
3. **Open SRE Agent** (sre.azure.com) — show it's monitoring
4. **Trigger a scenario** — `./tests/k6/run-scenario.sh http-500 --base-url <url>`
5. **Watch the script output** — shows which alerts to expect
6. **Watch Grafana** — see metrics change in real-time
7. **Watch SRE Agent** — see it detect, investigate, and correlate
8. **Repeat** with `--only` to trigger additional scenarios
