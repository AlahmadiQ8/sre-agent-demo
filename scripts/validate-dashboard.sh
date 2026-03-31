#!/bin/bash
# validate-dashboard.sh — Deploys the Contoso Bank Grafana dashboard locally and validates it.
# Usage: ./scripts/validate-dashboard.sh
# Prerequisites: Docker, curl, python3
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
GRAFANA_DIR="$REPO_ROOT/grafana"
DASHBOARD_JSON="$GRAFANA_DIR/dashboards/contoso-bank.json"
GRAFANA_URL="http://localhost:3000"
PROMETHEUS_URL="http://localhost:9090"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

pass() { echo -e "${GREEN}  ✅ $1${NC}"; }
fail() { echo -e "${RED}  ❌ $1${NC}"; ERRORS=$((ERRORS + 1)); }
warn() { echo -e "${YELLOW}  ⚠️  $1${NC}"; }

ERRORS=0

echo "=== Contoso Bank Dashboard Validation ==="
echo ""

# 1. Validate JSON file exists and parses
echo "--- Step 1: JSON file validation ---"
if [ ! -f "$DASHBOARD_JSON" ]; then
  fail "Dashboard JSON not found: $DASHBOARD_JSON"
  exit 1
fi
pass "Dashboard JSON exists"

if python3 -c "import json; json.load(open('$DASHBOARD_JSON'))" 2>/dev/null; then
  pass "JSON parses correctly"
else
  fail "JSON parse error"
  exit 1
fi

# 2. Start local Grafana + Prometheus
echo ""
echo "--- Step 2: Deploy to local Grafana ---"
cd "$GRAFANA_DIR"

if ! docker compose up -d --quiet-pull 2>/dev/null; then
  fail "Docker Compose failed to start"
  exit 1
fi
pass "Docker Compose started (Grafana + Prometheus)"

echo "  Waiting for Grafana to be ready..."
for i in $(seq 1 30); do
  if curl -s -o /dev/null -w "%{http_code}" "$GRAFANA_URL/api/health" 2>/dev/null | grep -q "200"; then
    break
  fi
  if [ "$i" -eq 30 ]; then
    fail "Grafana did not become ready within 60 seconds"
    docker compose down -v 2>/dev/null
    exit 1
  fi
  sleep 2
done
pass "Grafana is healthy"

# 3. Validate datasource
echo ""
echo "--- Step 3: Datasource validation ---"
DS_COUNT=$(curl -s "$GRAFANA_URL/api/datasources" | python3 -c "import json,sys; print(len(json.load(sys.stdin)))")
if [ "$DS_COUNT" -ge 1 ]; then
  pass "Prometheus datasource provisioned ($DS_COUNT datasource(s))"
else
  fail "No datasources found"
fi

# 4. Validate dashboard was imported
echo ""
echo "--- Step 4: Dashboard deployment validation ---"
DASH_RESPONSE=$(curl -s "$GRAFANA_URL/api/dashboards/uid/contoso-bank")
DASH_TITLE=$(echo "$DASH_RESPONSE" | python3 -c "import json,sys; print(json.load(sys.stdin)['dashboard']['title'])" 2>/dev/null || echo "")
if [ "$DASH_TITLE" = "Contoso Bank" ]; then
  pass "Dashboard deployed: $DASH_TITLE"
else
  fail "Dashboard not found or title mismatch (got: '$DASH_TITLE')"
fi

# 5. Structural validation via Grafana API
echo ""
echo "--- Step 5: Dashboard structure validation ---"
python3 - "$GRAFANA_URL" <<'PYEOF'
import json, urllib.request, sys

url = f"{sys.argv[1]}/api/dashboards/uid/contoso-bank"
with urllib.request.urlopen(url) as resp:
    dash = json.loads(resp.read())["dashboard"]

errors = 0

# Required fields
for field in ["title", "uid", "panels", "templating", "time"]:
    if field in dash:
        print(f"  \033[0;32m✅ Required field: {field}\033[0m")
    else:
        print(f"  \033[0;31m❌ Missing field: {field}\033[0m")
        errors += 1

# Time range
time = dash.get("time", {})
if time.get("from") == "now-15m" and time.get("to") == "now":
    print(f"  \033[0;32m✅ Time range: 15-minute default\033[0m")
else:
    print(f"  \033[0;31m❌ Time range: expected now-15m/now, got {time}\033[0m")
    errors += 1

# Variables
variables = {v["name"]: v for v in dash.get("templating", {}).get("list", [])}
for var in ["datasource", "job"]:
    if var in variables:
        print(f"  \033[0;32m✅ Variable: {var} (type={variables[var]['type']})\033[0m")
    else:
        print(f"  \033[0;31m❌ Missing variable: {var}\033[0m")
        errors += 1

# Row sections
rows = [p for p in dash["panels"] if p["type"] == "row"]
row_titles = [r["title"] for r in rows]
for expected in ["Overview", "Infrastructure", "Database", "Business"]:
    if expected in row_titles:
        print(f"  \033[0;32m✅ Row section: {expected}\033[0m")
    else:
        print(f"  \033[0;31m❌ Missing row: {expected}\033[0m")
        errors += 1

# Panel count
data_panels = [p for p in dash["panels"] if p["type"] != "row"]
if len(data_panels) >= 17:
    print(f"  \033[0;32m✅ Data panels: {len(data_panels)}\033[0m")
else:
    print(f"  \033[0;31m❌ Expected ≥17 panels, got {len(data_panels)}\033[0m")
    errors += 1

# All panels have queries
for p in data_panels:
    targets = p.get("targets", [])
    if not targets:
        print(f"  \033[0;31m❌ Panel {p['id']} ({p['title']}) has no queries\033[0m")
        errors += 1

# PromQL metrics coverage
expected_metrics = [
    "http_server_request_duration_seconds", "http_server_active_requests",
    "process_cpu_seconds_total", "process_working_set_bytes",
    "dotnet_gc_collections_total", "dotnet_thread_pool_threads",
    "contosobank_db_connections_active", "contosobank_db_errors_total",
    "contosobank_dependency_timeouts_total", "contosobank_transfers_total",
    "contosobank_transactions_processed_total", "contosobank_exceptions_total",
    "contosobank_log_entries_total",
]
all_queries = " ".join(
    t.get("expr", "") for p in data_panels for t in p.get("targets", [])
)
for metric in expected_metrics:
    if metric in all_queries:
        print(f"  \033[0;32m✅ Metric: {metric}\033[0m")
    else:
        print(f"  \033[0;31m❌ Metric not queried: {metric}\033[0m")
        errors += 1

sys.exit(errors)
PYEOF
STRUCT_RESULT=$?
ERRORS=$((ERRORS + STRUCT_RESULT))

# 6. PromQL query resolution against Prometheus
echo ""
echo "--- Step 6: PromQL query resolution ---"
python3 - "$GRAFANA_URL" "$PROMETHEUS_URL" <<'PYEOF'
import json, urllib.request, urllib.parse, sys

grafana_url = sys.argv[1]
prom_url = sys.argv[2]
errors = 0

with urllib.request.urlopen(f"{grafana_url}/api/dashboards/uid/contoso-bank") as resp:
    dash = json.loads(resp.read())["dashboard"]

data_panels = [p for p in dash["panels"] if p["type"] != "row"]
for p in data_panels:
    for t in p.get("targets", []):
        expr = t.get("expr", "")
        if not expr:
            continue
        test_expr = expr.replace("$job", "contoso-bank").replace("${job}", "contoso-bank")
        test_expr = test_expr.replace("$__rate_interval", "5m").replace("${__rate_interval}", "5m")
        encoded = urllib.parse.quote(test_expr)
        try:
            with urllib.request.urlopen(f"{prom_url}/api/v1/query?query={encoded}") as resp:
                result = json.loads(resp.read())
                if result.get("status") == "success":
                    print(f"  \033[0;32m✅ Panel {p['id']}: query resolves\033[0m")
                else:
                    print(f"  \033[0;31m❌ Panel {p['id']}: query failed ({result.get('status')})\033[0m")
                    errors += 1
        except Exception as e:
            print(f"  \033[0;31m❌ Panel {p['id']}: query error ({e})\033[0m")
            errors += 1
        break  # test first query per panel

sys.exit(errors)
PYEOF
QUERY_RESULT=$?
ERRORS=$((ERRORS + QUERY_RESULT))

# 7. Cleanup
echo ""
echo "--- Step 7: Cleanup ---"
cd "$GRAFANA_DIR"
docker compose down -v 2>/dev/null
pass "Docker Compose stopped"

# Summary
echo ""
echo "================================="
if [ "$ERRORS" -eq 0 ]; then
  echo -e "${GREEN}Dashboard validation PASSED${NC}"
  exit 0
else
  echo -e "${RED}Dashboard validation FAILED ($ERRORS error(s))${NC}"
  exit 1
fi
