#!/bin/bash
# Post-provision script for Contoso Bank SRE Agent Demo
# Called automatically by azd after infrastructure provisioning
#
# This script will be fully implemented in Task 15. Placeholder for now.

set -euo pipefail

echo "=== Contoso Bank Post-Provision ==="
echo ""
echo "Environment: ${AZURE_ENV_NAME:-unknown}"
echo "Resource Group: ${AZURE_RESOURCE_GROUP:-unknown}"
echo ""

# Grant managed identity access to SQL database
if [ -n "${SQL_SERVER_FQDN:-}" ] && [ -n "${MANAGED_IDENTITY_NAME:-}" ]; then
  echo "Granting managed identity '${MANAGED_IDENTITY_NAME}' access to SQL database..."
  sqlcmd -S "$SQL_SERVER_FQDN" -d "${SQL_DATABASE_NAME:-contoso-bank}" \
    --authentication-method ActiveDirectoryDefault \
    -Q "
      IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '${MANAGED_IDENTITY_NAME}')
      BEGIN
        CREATE USER [${MANAGED_IDENTITY_NAME}] FROM EXTERNAL PROVIDER;
        ALTER ROLE db_owner ADD MEMBER [${MANAGED_IDENTITY_NAME}];
        PRINT 'Managed identity granted db_owner access.';
      END
      ELSE
        PRINT 'Managed identity already has access.';
    " || echo "WARNING: Could not grant SQL access. You may need to run this manually."
else
  echo "WARNING: SQL_SERVER_FQDN or MANAGED_IDENTITY_NAME not set. Skipping SQL access grant."
fi

echo ""
echo "TODO (Task 15): Database seeding, Grafana data sources, dashboard import"
echo ""
echo "Grafana MCP Endpoint: ${GRAFANA_MCP_ENDPOINT:-not available}"
echo ""
echo "=== Post-Provision Complete ==="
