# Contoso Bank — k6 Traffic Simulation for Demo Alert Triggering

> **Status: COMPLETE ✅**
>
> **Depends on:** `01-spec.md` (complete) — all existing chaos scenarios, API endpoints, observability pipeline, and Azure Monitor alert rules (A1–A10) are in place.

## Overview

During the SRE Agent demo, the presenter needs to reliably fire Azure Monitor alerts (A1–A10) so SRE Agent can investigate them. Manually clicking chaos buttons in the UI and waiting is slow and error-prone. This spec adds **k6 load testing scripts** that automate traffic generation to trigger each alert within 1–2 minutes, with fine-grained control over which scenarios to activate.

### What This Adds

1. **Baseline traffic script** — Background traffic simulating normal banking users, keeping Grafana dashboards alive with realistic metrics
2. **Chaos trigger script** — Fires chaos endpoints and generates follow-up traffic to cross alert thresholds, with per-scenario enable/disable toggles
3. **Helper shell script** — One-liner wrapper with `--only`, `--disable`, `--dry-run` flags for easy demo orchestration
4. **Updated demo docs** — DEMO.md section explaining k6 usage

### What This Does NOT Change

- No modifications to existing application code, controllers, or ChaosService
- No changes to infrastructure (Bicep), Grafana dashboards, or alert rules
- No new NuGet packages or .NET dependencies

---

## Alert Threshold Analysis

Each Azure Monitor alert (defined in `infra/modules/alerts.bicep`) has specific thresholds. The k6 scripts must generate enough traffic to cross them:

| Alert | Threshold | k6 Strategy |
|-------|-----------|-------------|
| A1: High Memory (>80%) | Platform metric | Trigger `POST /api/reports/annual-statement` — self-sustaining (memory grows until OOM) |
| A2: OOM Restart | Container log event | Same trigger as A1 — eventually causes OOM kill |
| A3: High CPU (>90%) | Platform metric | Trigger `POST /api/accounts/fraud-detection` — self-sustaining (CPU-bound loop) |
| A4: HTTP 5xx > 10/5min | >10 errors in window | Trigger `POST /api/transfers/wire`, then hammer `/api/transfers/*` to accumulate 5xx responses |
| A5: DB Failures > 5/5min | >5 SQL failures | Trigger `POST /api/accounts/refresh`, then hammer DB-dependent endpoints (`/api/accounts`, `/api/transactions`) |
| A6: P95 Latency > 10s | P95 > 10,000ms | Trigger `POST /api/transfers/international`, then send concurrent requests (each takes 30s) |
| A7: Dep Timeout > 3/5min | >3 timeouts > 30s | Trigger `POST /api/settings/verify-identity` repeatedly (each ties up a thread) |
| A8: Log Volume > 5000/5min | >5000 log entries | Trigger `POST /api/transactions/export` — self-sustaining (floods logs continuously) |
| A9: Exceptions > 50/5min | >50 exceptions | Trigger `POST /api/reports/reconciliation` — self-sustaining (parallel exception tasks) |
| A10: Health Degraded | >3 failed health checks | Side effect of A5 (DB failure) — poll `/health/ready` to register failures |

---

## New Files

```
tests/k6/
├── baseline-traffic.js    # Background traffic (8 VUs, normal banking ops)
├── trigger-alerts.js      # Chaos trigger + follow-up traffic (per-scenario)
└── run-scenario.sh        # Helper wrapper with --only/--disable/--dry-run flags
```

---

## Implementation Plan — Dependency-Ordered Tasks

### Phase 1: k6 Scripts (no dependencies between tasks 1 and 2)

| # | Task | Description |
|---|------|-------------|
| 1 | **Create baseline traffic script** | Create `tests/k6/baseline-traffic.js`. Simulates normal banking users with 3 k6 scenarios: **`browse`** (constant-vus, 5 VUs) — cycles through `GET /api/accounts`, `GET /api/transactions`, `GET /api/transfers`, `GET /api/settings/profile` with 2–5s sleep between requests; **`transact`** (constant-vus, 2 VUs) — `POST /api/transfers` with valid transfer bodies using seeded account IDs 1–5, 5–10s sleep; **`health`** (constant-vus, 1 VU) — `GET /health/ready` + `GET /health/live` every 10s. Configurable via env vars: `BASE_URL` (default: `http://localhost:5170`), `DURATION` (default: `30m`). Add k6 checks for status 200 on all responses. No pass/fail thresholds — this is background traffic only. |
| 2 | **Create chaos trigger script** | Create `tests/k6/trigger-alerts.js`. Reads `ENABLED_SCENARIOS` env var (comma-separated list of scenario names) and `BASE_URL` env var. Dynamically builds the k6 `options.scenarios` object to only include enabled scenarios. Each scenario uses a `shared-iterations` executor with appropriate VU count and iteration count. Every scenario has two stages: (1) a `setup`-like initial iteration that POSTs to the chaos trigger endpoint, then (2) follow-up iterations that generate traffic to cross alert thresholds. **Scenario details:** `memory-leak` — POST `/api/reports/annual-statement?accountId=1`, then poll `GET /health/ready` every 5s for 3 min; `cpu-spike` — POST `/api/accounts/fraud-detection`, then send `GET /api/accounts` every 2s for 3 min to show latency impact; `http-500` — POST `/api/transfers/wire` with body `{"fromAccountId":1,"toAccountId":2,"amount":100}`, then rapid-fire the same POST every 1s (need >10 failures); `db-failure` — POST `/api/accounts/refresh`, then rapid `GET /api/accounts` + `GET /api/transactions` every 1s, plus poll `GET /health/ready`; `slow-api` — POST `/api/transfers/international` with body `{"fromAccountId":1,"toAccountId":2,"amount":500}` using 5 concurrent VUs (each request takes 30s, stacking P95); `dependency-timeout` — POST `/api/settings/verify-identity` 5 times with 10s gaps (each ties up a thread, >3 needed); `log-flood` — POST `/api/transactions/export`, then send `GET /api/accounts` every 2s to show sluggishness; `exception-storm` — POST `/api/reports/reconciliation` 5 times in quick succession (each spawns parallel exception tasks, need >50 total). Default duration per scenario: 3 minutes (configurable via `DURATION` env var). When multiple scenarios are enabled, they run sequentially with a 30s gap between them. |
| 2t | **Test: k6 scripts validate** | Verify both k6 scripts pass syntax validation: `k6 inspect tests/k6/baseline-traffic.js` and `k6 inspect tests/k6/trigger-alerts.js` (if k6 is installed). If k6 is not installed, verify the JS files parse without syntax errors using `node --check`. |

### Phase 2: Helper Script (depends on Phase 1)

| # | Task | Description |
|---|------|-------------|
| 3 | **Create helper shell script with per-scenario toggles** | Create `tests/k6/run-scenario.sh` (executable, `chmod +x`). Accepts a positional argument: a single scenario name (e.g., `memory-leak`) or `all`. Supports flags: `--disable <scenario>[,<scenario>...]` — remove specific scenarios (only meaningful with `all`); `--only <scenario>[,<scenario>...]` — enable ONLY the listed scenarios (disables everything else); `--base-url <url>` — override target URL (default: `http://localhost:5170`); `--duration <time>` — override run duration (default: `3m`); `--list` — print all valid scenario names and exit; `--dry-run` — print the k6 command that would execute without running it. Valid scenario names: `memory-leak`, `cpu-spike`, `http-500`, `db-failure`, `slow-api`, `dependency-timeout`, `log-flood`, `exception-storm`. The script validates the scenario name, checks that k6 is installed (exits with helpful install message if not), builds the `ENABLED_SCENARIOS` env var, prints which alerts to expect (e.g., "→ Expecting alerts: A4 (HTTP 5xx), A10 (Health)"), then executes `k6 run` with the appropriate env vars. Also supports `baseline` as the positional argument to run `baseline-traffic.js` instead. |
| 3t | **Test: shell script** | Verify `./tests/k6/run-scenario.sh --list` prints all 8 scenario names. Verify `./tests/k6/run-scenario.sh --dry-run all` prints the k6 command without executing. Verify `./tests/k6/run-scenario.sh --dry-run all --only http-500,db-failure` shows only those two in `ENABLED_SCENARIOS`. Verify invalid scenario name exits with error. |

### Phase 3: Documentation (depends on Phase 2)

| # | Task | Description |
|---|------|-------------|
| 4 | **Update DEMO.md with k6 traffic simulation section** | Add a "Traffic Simulation with k6" section to `DEMO.md`. Include: prerequisites (`brew install k6`), quick-start examples (baseline traffic, single scenario, all scenarios, cherry-pick with `--only`/`--disable`), alert-to-scenario mapping table, and a recommended demo flow that incorporates k6 (start baseline → open Grafana → trigger scenario → watch alerts → SRE Agent investigates). |

---

## Usage Examples (for reference during implementation)

```bash
# Prerequisites
brew install k6    # macOS
# or: go install go.k6.io/k6@latest

# Start baseline traffic (run before demo, keep running in background)
./tests/k6/run-scenario.sh baseline

# Trigger a single chaos scenario
./tests/k6/run-scenario.sh memory-leak
./tests/k6/run-scenario.sh http-500

# Trigger all chaos scenarios sequentially
./tests/k6/run-scenario.sh all

# Trigger all except memory-leak and cpu-spike
./tests/k6/run-scenario.sh all --disable memory-leak,cpu-spike

# Trigger only http-500 and db-failure together
./tests/k6/run-scenario.sh all --only http-500,db-failure

# Target Azure deployment instead of localhost
./tests/k6/run-scenario.sh all --base-url https://contoso-bank.azurecontainerapps.io

# Preview what would run without executing
./tests/k6/run-scenario.sh all --only http-500,slow-api --dry-run

# List available scenarios
./tests/k6/run-scenario.sh --list
```

---

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| **k6** (not Artillery, Locust, JMeter) | JavaScript-based (familiar to .NET devs), single binary, excellent CLI UX, built-in scenarios/executors, no JVM dependency |
| **Two scripts** (baseline + trigger) | Separation of concerns: baseline runs continuously in background, trigger is fire-and-forget per demo scenario |
| **`ENABLED_SCENARIOS` env var** (not CLI args to k6) | k6's `options.scenarios` is configured in JS — env var is the standard way to pass runtime config to k6 scripts |
| **`--only` and `--disable` flags** (not `--enable`) | Subtractive (`--disable`) is convenient when you want "all but one". Additive (`--only`) is convenient when you want "just these two". Both patterns covered. |
| **Sequential scenario execution** (not parallel) | Parallel chaos would overwhelm the app and make it hard to attribute alerts to specific scenarios. Sequential with 30s gaps lets each alert fire cleanly. |
| **3-minute default duration** | Alert evaluation window is 5 min with 1 min frequency. 3 minutes of sustained traffic ensures at least 2 evaluation cycles — enough for the alert to fire. |
| **No k6 thresholds** | These scripts are traffic generators, not performance tests. We don't want k6 to "fail" because the chaos scenario caused errors — that's the point. |
| **Shell wrapper** (not npm/Makefile) | Zero dependencies beyond k6 and bash. Works on macOS, Linux, and WSL. No build step needed. |

---

## Notes

- k6 scripts use tagged template literals (`http.url`) for URL grouping to keep k6's built-in metrics clean
- Transfer POST bodies use account IDs 1–5 from `src/ContosoBank/Data/SeedData.cs`
- Self-sustaining scenarios (memory leak, CPU spike, log flood, exception storm) only need a single trigger POST — k6's follow-up traffic is just to show impact on dashboards, not to sustain the failure
- For Azure deployment: set `--base-url` to the Container App's FQDN (found in `azd env get-values | grep SERVICE_CONTOSOBANK_URL`)

