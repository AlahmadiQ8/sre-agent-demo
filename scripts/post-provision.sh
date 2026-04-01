#!/bin/bash
# Post-provision script for Contoso Bank SRE Agent Demo
# Called automatically by azd after infrastructure provisioning

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="${SCRIPT_DIR}/.."

echo "=== Contoso Bank Post-Provision ==="
echo ""
echo "Environment: ${AZURE_ENV_NAME:-unknown}"
echo "Resource Group: ${AZURE_RESOURCE_GROUP:-unknown}"
echo ""

# Grant managed identity access to SQL database
if [ -n "${SQL_SERVER_FQDN:-}" ] && [ -n "${MANAGED_IDENTITY_NAME:-}" ]; then
  SQL_SERVER_NAME="${SQL_SERVER_FQDN%%.*}"
  MY_IP=$(curl -s https://api.ipify.org 2>/dev/null || echo "")

  if [ -n "$MY_IP" ]; then
    echo "Adding temporary firewall rule for $MY_IP..."
    az sql server firewall-rule create \
      --server "$SQL_SERVER_NAME" \
      --resource-group "$AZURE_RESOURCE_GROUP" \
      --name PostProvisionAccess \
      --start-ip-address "$MY_IP" \
      --end-ip-address "$MY_IP" \
      --output none 2>/dev/null || true
  fi

  echo "Granting managed identity '${MANAGED_IDENTITY_NAME}' access to SQL database..."
  sqlcmd -S "$SQL_SERVER_FQDN" -d "${SQL_DATABASE_NAME:-contoso-bank}" \
    --authentication-method ActiveDirectoryDefault \
    -Q "
      IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '${MANAGED_IDENTITY_NAME}')
        CREATE USER [${MANAGED_IDENTITY_NAME}] FROM EXTERNAL PROVIDER;
      ALTER ROLE db_datareader ADD MEMBER [${MANAGED_IDENTITY_NAME}];
      ALTER ROLE db_datawriter ADD MEMBER [${MANAGED_IDENTITY_NAME}];
      ALTER ROLE db_ddladmin ADD MEMBER [${MANAGED_IDENTITY_NAME}];
    " 2>/dev/null && echo "✅ Managed identity granted SQL access." \
    || echo "⚠️  Could not grant SQL access. Run manually after provisioning."

  # Execute seed data script against Azure SQL Database
  SEED_SCRIPT="${SCRIPT_DIR}/seed-data.sql"
  if [ -f "$SEED_SCRIPT" ]; then
    echo ""
    echo "Seeding database with demo data..."
    sqlcmd -S "$SQL_SERVER_FQDN" -d "${SQL_DATABASE_NAME:-contoso-bank}" \
      --authentication-method ActiveDirectoryDefault \
      -i "$SEED_SCRIPT" 2>/dev/null \
      && echo "✅ Database seeded with demo data." \
      || echo "⚠️  Could not seed database. Run manually: sqlcmd -S $SQL_SERVER_FQDN -d ${SQL_DATABASE_NAME:-contoso-bank} -i scripts/seed-data.sql"
  else
    echo "⚠️  Seed script not found: ${SEED_SCRIPT}"
  fi

  if [ -n "$MY_IP" ]; then
    echo "Removing temporary firewall rule..."
    az sql server firewall-rule delete \
      --server "$SQL_SERVER_NAME" \
      --resource-group "$AZURE_RESOURCE_GROUP" \
      --name PostProvisionAccess \
      --yes --output none 2>/dev/null || true
  fi
else
  echo "⚠️  SQL_SERVER_FQDN or MANAGED_IDENTITY_NAME not set. Skipping SQL access grant."
fi

# ============================================================
# Container Apps OTel Agent Configuration
# Configure managed OpenTelemetry agent to forward metrics
# ============================================================

echo ""
echo "--- Container Apps OTel Agent ---"
echo ""

CAE_NAME="${CONTAINER_APPS_ENVIRONMENT_NAME:-}"

if [ -n "$CAE_NAME" ] && [ -n "${AZURE_RESOURCE_GROUP:-}" ] && [ -n "${APPLICATIONINSIGHTS_CONNECTION_STRING:-}" ]; then
  echo "Configuring managed OpenTelemetry agent on environment '${CAE_NAME}'..."

  # Configure App Insights telemetry (traces + logs)
  az containerapp env telemetry app-insights set \
    --name "$CAE_NAME" \
    --resource-group "$AZURE_RESOURCE_GROUP" \
    --connection-string "$APPLICATIONINSIGHTS_CONNECTION_STRING" \
    --enable-open-telemetry-traces true \
    --enable-open-telemetry-logs true \
    --output none 2>/dev/null \
    && echo "✅ App Insights telemetry configured." \
    || echo "⚠️  Could not configure App Insights telemetry."
else
  echo "⚠️  Missing environment variables. Skipping OTel agent configuration."
fi

# ============================================================
# Grafana Configuration
# Configure data sources, import dashboard, verify scraping
# ============================================================

echo ""
echo "--- Grafana Data Sources & Dashboard ---"
echo ""

DASHBOARD_FILE="${REPO_ROOT}/grafana/dashboards/contoso-bank.json"
GRAFANA_NAME="${GRAFANA_NAME:-}"
SUBSCRIPTION_ID="${AZURE_SUBSCRIPTION_ID:-}"

if [ -z "$GRAFANA_NAME" ]; then
  echo "⚠️  GRAFANA_NAME not set. Skipping Grafana configuration."
elif [ -z "${AZURE_RESOURCE_GROUP:-}" ]; then
  echo "⚠️  AZURE_RESOURCE_GROUP not set. Skipping Grafana configuration."
else
  # Resolve subscription ID if not set
  if [ -z "$SUBSCRIPTION_ID" ]; then
    SUBSCRIPTION_ID=$(az account show --query id -o tsv 2>/dev/null || echo "")
  fi

  if [ -z "$SUBSCRIPTION_ID" ]; then
    echo "⚠️  Could not determine Azure subscription ID. Skipping Grafana configuration."
  else
    # Wait for Grafana to be fully provisioned (RBAC can take a few minutes)
    echo "Waiting for Grafana instance '${GRAFANA_NAME}' to be ready..."
    GRAFANA_READY=false
    for i in $(seq 1 30); do
      STATUS=$(az grafana show \
        --name "$GRAFANA_NAME" \
        --resource-group "$AZURE_RESOURCE_GROUP" \
        --query "properties.provisioningState" -o tsv 2>/dev/null || echo "")
      if [ "$STATUS" = "Succeeded" ]; then
        echo "✅ Grafana instance is ready."
        GRAFANA_READY=true
        break
      fi
      echo "  Grafana status: ${STATUS:-unknown} (attempt $i/30, retrying in 10s...)"
      sleep 10
    done

    if [ "$GRAFANA_READY" = false ]; then
      echo "⚠️  Grafana not ready after 5 minutes. Attempting configuration anyway..."
    fi

    # --- Data Source 1: Azure Monitor Managed Prometheus ---
    # Bicep's azureMonitorWorkspaceIntegrations auto-creates a data source using the
    # deprecated core "prometheus" type. Replace it with the dedicated
    # "grafana-azureprometheus-datasource" plugin to eliminate the deprecation warning.
    echo ""
    echo "Configuring Prometheus data source..."

    # Read the Prometheus query endpoint URL from the auto-configured data source
    PROM_URL=$(az grafana data-source list \
      --name "$GRAFANA_NAME" \
      --resource-group "$AZURE_RESOURCE_GROUP" \
      --query "[?type=='prometheus'].url | [0]" -o tsv 2>/dev/null || echo "")
    PROM_DS_NAME=$(az grafana data-source list \
      --name "$GRAFANA_NAME" \
      --resource-group "$AZURE_RESOURCE_GROUP" \
      --query "[?type=='prometheus'].name | [0]" -o tsv 2>/dev/null || echo "")

    if [ -n "$PROM_DS_NAME" ]; then
      echo "  Found deprecated Prometheus data source: ${PROM_DS_NAME}"
      echo "  Prometheus URL: ${PROM_URL}"
      echo "  Deleting deprecated data source..."
      az grafana data-source delete \
        --name "$GRAFANA_NAME" \
        --resource-group "$AZURE_RESOURCE_GROUP" \
        --data-source "$PROM_DS_NAME" \
        --output none 2>/dev/null \
        && echo "  ✅ Deprecated data source deleted." \
        || echo "  ⚠️  Could not delete deprecated data source."
    fi

    # Check if the correct data source already exists
    EXISTING_PROM=$(az grafana data-source list \
      --name "$GRAFANA_NAME" \
      --resource-group "$AZURE_RESOURCE_GROUP" \
      --query "[?type=='grafana-azureprometheus-datasource'].name | [0]" -o tsv 2>/dev/null || echo "")

    if [ -n "$EXISTING_PROM" ]; then
      echo "  ✅ Azure Prometheus data source already exists: ${EXISTING_PROM}"
    elif [ -n "$PROM_URL" ]; then
      PROM_DEFINITION="{
  \"name\": \"Managed Prometheus\",
  \"type\": \"grafana-azureprometheus-datasource\",
  \"access\": \"proxy\",
  \"url\": \"${PROM_URL}\",
  \"isDefault\": true,
  \"jsonData\": {
    \"azureCredentials\": {
      \"authType\": \"msi\"
    },
    \"httpMethod\": \"POST\"
  }
}"
      az grafana data-source create \
        --name "$GRAFANA_NAME" \
        --resource-group "$AZURE_RESOURCE_GROUP" \
        --definition "$PROM_DEFINITION" \
        --output none 2>/dev/null \
        && echo "  ✅ Azure Prometheus data source created (grafana-azureprometheus-datasource)." \
        || echo "  ⚠️  Could not create Azure Prometheus data source."
    else
      echo "  ⚠️  No Prometheus URL found. Cannot create data source."
      echo "     Verify azureMonitorWorkspaceIntegrations in grafana.bicep."
    fi

    # --- Data Source 2: Azure Monitor (platform metrics + App Insights) ---
    echo ""
    echo "Configuring Azure Monitor data source..."

    AM_DEFINITION="{
  \"name\": \"Azure Monitor\",
  \"type\": \"grafana-azure-monitor-datasource\",
  \"access\": \"proxy\",
  \"jsonData\": {
    \"azureAuthType\": \"msi\",
    \"subscriptionId\": \"${SUBSCRIPTION_ID}\"
  }
}"

    EXISTING_AM=$(az grafana data-source show \
      --name "$GRAFANA_NAME" \
      --resource-group "$AZURE_RESOURCE_GROUP" \
      --data-source "Azure Monitor" \
      --query "name" -o tsv 2>/dev/null || echo "")

    if [ -n "$EXISTING_AM" ]; then
      az grafana data-source update \
        --name "$GRAFANA_NAME" \
        --resource-group "$AZURE_RESOURCE_GROUP" \
        --data-source "Azure Monitor" \
        --definition "$AM_DEFINITION" \
        --output none 2>/dev/null \
        && echo "✅ Azure Monitor data source updated." \
        || echo "⚠️  Could not update Azure Monitor data source."
    else
      az grafana data-source create \
        --name "$GRAFANA_NAME" \
        --resource-group "$AZURE_RESOURCE_GROUP" \
        --definition "$AM_DEFINITION" \
        --output none 2>/dev/null \
        && echo "✅ Azure Monitor data source created." \
        || echo "⚠️  Could not create Azure Monitor data source."
    fi

    # --- Data Source 3: Log Analytics (KQL queries against app/container logs) ---
    echo ""
    echo "Configuring Log Analytics data source..."

    LOG_ANALYTICS_WORKSPACE_ID="${AZURE_LOG_ANALYTICS_WORKSPACE_ID:-}"

    if [ -n "$LOG_ANALYTICS_WORKSPACE_ID" ]; then
      LA_DEFINITION="{
  \"name\": \"Log Analytics\",
  \"type\": \"grafana-azure-monitor-datasource\",
  \"access\": \"proxy\",
  \"jsonData\": {
    \"azureAuthType\": \"msi\",
    \"subscriptionId\": \"${SUBSCRIPTION_ID}\",
    \"logAnalyticsDefaultWorkspace\": \"${LOG_ANALYTICS_WORKSPACE_ID}\"
  }
}"

      EXISTING_LA=$(az grafana data-source show \
        --name "$GRAFANA_NAME" \
        --resource-group "$AZURE_RESOURCE_GROUP" \
        --data-source "Log Analytics" \
        --query "name" -o tsv 2>/dev/null || echo "")

      if [ -n "$EXISTING_LA" ]; then
        az grafana data-source update \
          --name "$GRAFANA_NAME" \
          --resource-group "$AZURE_RESOURCE_GROUP" \
          --data-source "Log Analytics" \
          --definition "$LA_DEFINITION" \
          --output none 2>/dev/null \
          && echo "✅ Log Analytics data source updated." \
          || echo "⚠️  Could not update Log Analytics data source."
      else
        az grafana data-source create \
          --name "$GRAFANA_NAME" \
          --resource-group "$AZURE_RESOURCE_GROUP" \
          --definition "$LA_DEFINITION" \
          --output none 2>/dev/null \
          && echo "✅ Log Analytics data source created." \
          || echo "⚠️  Could not create Log Analytics data source."
      fi
    else
      echo "⚠️  AZURE_LOG_ANALYTICS_WORKSPACE_ID not set. Skipping Log Analytics data source."
    fi

    # --- Import Grafana Dashboard ---
    echo ""
    echo "Importing Contoso Bank Grafana dashboard..."

    if [ -f "$DASHBOARD_FILE" ]; then
      az grafana dashboard create \
        --name "$GRAFANA_NAME" \
        --resource-group "$AZURE_RESOURCE_GROUP" \
        --definition @"$DASHBOARD_FILE" \
        --overwrite \
        --output none 2>/dev/null \
        && echo "✅ Dashboard 'Contoso Bank' imported successfully." \
        || echo "⚠️  Could not import dashboard. Import manually via Grafana UI."
    else
      echo "⚠️  Dashboard file not found: ${DASHBOARD_FILE}"
      echo "   Expected at: grafana/dashboards/contoso-bank.json"
    fi

    # --- Metrics Pipeline Summary ---
    echo ""
    echo "Metrics pipeline:"
    echo "  ✅ App exports metrics via UseAzureMonitor() → App Insights"
    echo "  ✅ Azure Monitor Workspace created (monitor-workspace.bicep)"
    echo "  ✅ Grafana dashboard queries AppRequests/AppMetrics via KQL"
    echo "  ✅ Prometheus /metrics endpoint available for local development"
  fi
fi

# ============================================================
# Summary & SRE Agent Setup Instructions
# ============================================================

echo ""
echo "--- Post-Provision Summary ---"
echo ""

GRAFANA_URL="${GRAFANA_ENDPOINT:-not available}"
MCP_URL="${GRAFANA_MCP_ENDPOINT:-not available}"

echo "  Grafana URL:         ${GRAFANA_URL}"
echo "  Grafana MCP Endpoint: ${MCP_URL}"
echo "  App URL:             ${API_URL:-not available}"
echo ""

if [ "$MCP_URL" != "not available" ]; then
  echo "--- SRE Agent Grafana Connector Setup ---"
  echo ""
  echo "  1. Go to sre.azure.com → Builder → Connectors"
  echo "  2. Click + Add connector → MCP Server"
  echo "  3. Enter MCP URL: ${MCP_URL}"
  echo "  4. Auth: Managed Identity"
  echo "  5. Select tools → Select all"
  echo "  6. Save → Status should show Connected"
  echo ""
fi

echo "=== Post-Provision Complete ==="
