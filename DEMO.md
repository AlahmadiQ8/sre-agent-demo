# Contoso Bank — Chaos Demo Cheat Sheet

Quick reference for triggering failure scenarios during live demos.

## How It Works

Every chaos scenario is triggered by a **normal-looking banking button** — there's no separate chaos panel or CLI. The audience sees a realistic app breaking naturally, then you switch to Grafana / SRE Agent to watch it investigate.

All scenarios **auto-recover after 5 minutes**, so you can repeat the demo.

## Chaos Trigger Map

| # | Page | Button to Click | Failure Triggered | What Audience Sees | What SRE Agent Finds |
|---|------|----------------|-------------------|--------------------|-----------------------|
| 1 | **Reports** | Generate Annual Statement | Memory Leak | Loading → timeout/error | Memory growth, OOM kill, `process_working_set_bytes` spike |
| 2 | **Dashboard** | Run Fraud Detection | CPU Spike | "Scanning transactions..." | CPU > 95%, thread starvation, `process_cpu_seconds_total` spike |
| 3 | **Transfers** | Wire Transfer | HTTP 500 Errors | "Transfer failed" error | 5xx spike on `/api/transfers/wire`, exception traces |
| 4 | **Accounts** | Refresh | DB Connection Failure | "Unable to load account information" | `SqlException` flood, `contosobank_db_errors_total` spike |
| 5 | **Transfers** | International Transfer | Slow API (30s delay) | Very slow loading | P99 latency spike, no CPU/memory issue → external dependency |
| 6 | **Settings** | Verify Identity (KYC) | Dependency Timeout | "Identity verification failed" | Timeout exceptions, `contosobank_dependency_timeouts_total` |
| 7 | **Transactions** | Export Full History | Log Flooding | "Preparing export..." → sluggish app | Log volume spike, `contosobank_log_entries_total` rockets |
| 8 | **Reports** | Run Batch Reconciliation | Exception Storm | "Reconciliation failed" | Multiple exception types, `contosobank_exceptions_total` spike |

## Suggested Demo Flow

1. **Open the app** in browser — show it's a working banking app
2. **Open Grafana** side-by-side — show healthy dashboards
3. **Open SRE Agent** (sre.azure.com) — show it's monitoring
4. **Click a button** (e.g., "Generate Annual Statement" on Reports)
5. **Watch Grafana** — see metrics change in real-time
6. **Watch SRE Agent** — see it detect, investigate, and correlate
7. **Repeat** with a different scenario to show breadth

## Best Scenarios to Start With

- **Memory Leak** (Reports → Annual Statement) — dramatic, visual in Grafana
- **HTTP 500** (Transfers → Wire Transfer) — fast, immediate error spike
- **Exception Storm** (Reports → Batch Reconciliation) — shows diverse exception analysis
