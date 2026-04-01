#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# ── Defaults ─────────────────────────────────────────────────────────────────
BASE_URL="http://localhost:5170"
DURATION="3m"
DRY_RUN=false

VALID_SCENARIOS=(
  memory-leak
  cpu-spike
  http-500
  db-failure
  slow-api
  dependency-timeout
  log-flood
  exception-storm
)

# Alert mapping: scenario → expected alerts
alert_for() {
  case "$1" in
    memory-leak)        echo "A1 (High Memory), A2 (OOM Restart)" ;;
    cpu-spike)          echo "A3 (High CPU)" ;;
    http-500)           echo "A4 (HTTP 5xx)" ;;
    db-failure)         echo "A5 (DB Failures), A10 (Health Degraded)" ;;
    slow-api)           echo "A6 (P95 Latency)" ;;
    dependency-timeout) echo "A7 (Dependency Timeout)" ;;
    log-flood)          echo "A8 (Log Volume)" ;;
    exception-storm)    echo "A9 (Exception Storm)" ;;
  esac
}

# ── Helpers ──────────────────────────────────────────────────────────────────
usage() {
  cat <<EOF
Usage: $(basename "$0") <scenario|all|baseline> [flags]

Positional arguments:
  baseline                     Run baseline traffic (background banking ops)
  <scenario>                   Run a single chaos scenario
  all                          Run all chaos scenarios sequentially

Flags:
  --base-url <url>             Target URL (default: $BASE_URL)
  --duration <time>            Duration per scenario (default: $DURATION)
  --only <s1>[,<s2>,...]       Enable ONLY the listed scenarios (use with 'all')
  --disable <s1>[,<s2>,...]    Disable specific scenarios (use with 'all')
  --list                       Print all valid scenario names and exit
  --dry-run                    Print the k6 command without executing
  -h, --help                   Show this help message

Valid scenarios:
  $(printf '  %s\n' "${VALID_SCENARIOS[@]}")
EOF
}

is_valid_scenario() {
  local name="$1"
  for s in "${VALID_SCENARIOS[@]}"; do
    [[ "$s" == "$name" ]] && return 0
  done
  return 1
}

validate_scenario_list() {
  local label="$1"
  shift
  for name in "$@"; do
    if ! is_valid_scenario "$name"; then
      echo "Error: Invalid scenario '$name' in $label." >&2
      echo "Valid scenarios: ${VALID_SCENARIOS[*]}" >&2
      exit 1
    fi
  done
}

# ── Parse arguments ──────────────────────────────────────────────────────────
POSITIONAL=""
ONLY_LIST=()
DISABLE_LIST=()

while [[ $# -gt 0 ]]; do
  case "$1" in
    --list)
      printf '%s\n' "${VALID_SCENARIOS[@]}"
      exit 0
      ;;
    --base-url)
      BASE_URL="$2"; shift 2 ;;
    --duration)
      DURATION="$2"; shift 2 ;;
    --only)
      IFS=',' read -ra ONLY_LIST <<< "$2"; shift 2 ;;
    --disable)
      IFS=',' read -ra DISABLE_LIST <<< "$2"; shift 2 ;;
    --dry-run)
      DRY_RUN=true; shift ;;
    -h|--help)
      usage; exit 0 ;;
    -*)
      echo "Error: Unknown flag '$1'" >&2; usage >&2; exit 1 ;;
    *)
      if [[ -n "$POSITIONAL" ]]; then
        echo "Error: Only one positional argument expected, got extra '$1'" >&2
        exit 1
      fi
      POSITIONAL="$1"; shift ;;
  esac
done

if [[ -z "$POSITIONAL" ]]; then
  echo "Error: Missing positional argument (scenario name, 'all', or 'baseline')." >&2
  usage >&2
  exit 1
fi

# ── Check k6 is installed ───────────────────────────────────────────────────
if ! command -v k6 &>/dev/null; then
  cat >&2 <<EOF
Error: k6 is not installed.

Install with:
  brew install k6          # macOS
  go install go.k6.io/k6@latest  # Go
  # or see https://grafana.com/docs/k6/latest/set-up/install-k6/
EOF
  exit 1
fi

# ── Handle 'baseline' ───────────────────────────────────────────────────────
if [[ "$POSITIONAL" == "baseline" ]]; then
  CMD=(k6 run
    --env "BASE_URL=$BASE_URL"
    --env "DURATION=$DURATION"
    "$SCRIPT_DIR/baseline-traffic.js"
  )
  echo "→ Running baseline traffic against $BASE_URL (duration: $DURATION)"
  if [[ "$DRY_RUN" == true ]]; then
    echo "[dry-run] ${CMD[*]}"
    exit 0
  fi
  exec "${CMD[@]}"
fi

# ── Build scenario list ─────────────────────────────────────────────────────
SCENARIOS=()

if [[ "$POSITIONAL" == "all" ]]; then
  if [[ ${#ONLY_LIST[@]} -gt 0 ]]; then
    validate_scenario_list "--only" "${ONLY_LIST[@]}"
    SCENARIOS=("${ONLY_LIST[@]}")
  else
    SCENARIOS=("${VALID_SCENARIOS[@]}")
    if [[ ${#DISABLE_LIST[@]} -gt 0 ]]; then
      validate_scenario_list "--disable" "${DISABLE_LIST[@]}"
      FILTERED=()
      for s in "${SCENARIOS[@]}"; do
        local_skip=false
        for d in "${DISABLE_LIST[@]}"; do
          [[ "$s" == "$d" ]] && local_skip=true && break
        done
        [[ "$local_skip" == false ]] && FILTERED+=("$s")
      done
      SCENARIOS=("${FILTERED[@]}")
    fi
  fi
else
  # Single scenario
  if ! is_valid_scenario "$POSITIONAL"; then
    echo "Error: Invalid scenario '$POSITIONAL'." >&2
    echo "Valid scenarios: ${VALID_SCENARIOS[*]}" >&2
    echo "Use --list to see all valid scenario names." >&2
    exit 1
  fi
  SCENARIOS=("$POSITIONAL")
fi

if [[ ${#SCENARIOS[@]} -eq 0 ]]; then
  echo "Error: No scenarios to run (all were disabled)." >&2
  exit 1
fi

# ── Print expected alerts ────────────────────────────────────────────────────
ENABLED_CSV=$(IFS=','; echo "${SCENARIOS[*]}")

echo "→ Scenarios: ${SCENARIOS[*]}"
echo "→ Target:    $BASE_URL"
echo "→ Duration:  $DURATION per scenario"
echo ""
echo "→ Expecting alerts:"
for s in "${SCENARIOS[@]}"; do
  echo "    $s → $(alert_for "$s")"
done
echo ""

# ── Build and run k6 command ─────────────────────────────────────────────────
CMD=(k6 run
  --env "BASE_URL=$BASE_URL"
  --env "DURATION=$DURATION"
  --env "ENABLED_SCENARIOS=$ENABLED_CSV"
  "$SCRIPT_DIR/trigger-alerts.js"
)

if [[ "$DRY_RUN" == true ]]; then
  echo "[dry-run] ${CMD[*]}"
  exit 0
fi

exec "${CMD[@]}"
