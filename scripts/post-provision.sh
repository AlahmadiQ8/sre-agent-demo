#!/bin/bash
# Post-provision script for Contoso Bank SRE Agent Demo
# Called automatically by azd after infrastructure provisioning

set -euo pipefail

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

echo ""
echo "TODO (Task 15): Database seeding, Grafana data sources, dashboard import"
echo ""
echo "Grafana MCP Endpoint: ${GRAFANA_MCP_ENDPOINT:-not available}"
echo ""
echo "=== Post-Provision Complete ==="
