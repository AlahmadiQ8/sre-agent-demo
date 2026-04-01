// Contoso Bank — Azure SRE Agent Demo
// Subscription-scoped entry point orchestrating all modules
// All inter-resource connectivity uses managed identity + RBAC (no credentials)

targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the azd environment')
param environmentName string

@minLength(1)
@description('Azure region for all resources')
param location string

@description('Principal ID of the deploying user (azd auto-populates)')
param principalId string = ''

var resourceSuffix = take(uniqueString(subscription().id, environmentName, location), 6)
var name = '${environmentName}-${resourceSuffix}'
var tags = { 'azd-env-name': environmentName }

resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

// 1. Managed Identity — created first, used by all other modules
module identity './modules/identity.bicep' = {
  name: 'identity'
  scope: rg
  params: {
    name: 'id-${name}'
    location: location
    tags: tags
  }
}

// 2. Monitoring — Log Analytics + App Insights
module monitoring './modules/monitoring.bicep' = {
  name: 'monitoring'
  scope: rg
  params: {
    name: name
    location: location
    tags: tags
  }
}

// 3. Container Apps Environment + ACR (MI-based image pulls)
module containerEnv './modules/container-env.bicep' = {
  name: 'container-env'
  scope: rg
  params: {
    name: name
    location: location
    tags: tags
    logAnalyticsWorkspaceName: monitoring.outputs.logAnalyticsWorkspaceName
    managedIdentityPrincipalId: identity.outputs.principalId
  }
}

// 4. Azure SQL — AAD-only auth, no SQL admin password
module sql './modules/sql.bicep' = {
  name: 'sql'
  scope: rg
  params: {
    name: name
    location: location
    tags: tags
    sqlAdminObjectId: principalId
    managedIdentityName: identity.outputs.name
  }
}

// 5. Azure Monitor Workspace — metrics backend for Grafana
module monitorWorkspace './modules/monitor-workspace.bicep' = {
  name: 'monitor-workspace'
  scope: rg
  params: {
    name: name
    location: location
    tags: tags
  }
}

// 6. Grafana — Dashboards + MCP endpoint for SRE Agent
module grafana './modules/grafana.bicep' = {
  name: 'grafana'
  scope: rg
  params: {
    name: name
    location: location
    tags: tags
    monitorWorkspaceId: monitorWorkspace.outputs.monitorWorkspaceId
    managedIdentityPrincipalId: identity.outputs.principalId
    principalId: principalId
  }
}

// 7. Container App — uses MI for ACR pull + SQL connection
module containerApp './modules/container-app.bicep' = {
  name: 'container-app'
  scope: rg
  params: {
    name: name
    location: location
    tags: tags
    containerAppsEnvironmentId: containerEnv.outputs.environmentId
    containerRegistryLoginServer: containerEnv.outputs.registryLoginServer
    managedIdentityId: identity.outputs.id
    managedIdentityClientId: identity.outputs.clientId
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    sqlServerFqdn: sql.outputs.serverFqdn
    sqlDatabaseName: sql.outputs.databaseName
  }
}

// 8. Alert Rules — 10 pre-configured alerts mapped to 8 chaos scenarios
module alerts './modules/alerts.bicep' = {
  name: 'alerts'
  scope: rg
  params: {
    name: name
    location: location
    tags: tags
    containerAppId: containerApp.outputs.id
    containerAppName: containerApp.outputs.name
    appInsightsId: monitoring.outputs.appInsightsId
    logAnalyticsWorkspaceId: monitoring.outputs.logAnalyticsWorkspaceId
  }
}

// ============================================================
// Outputs — UPPERCASE names become azd environment variables
// ============================================================
output AZURE_RESOURCE_GROUP string = rg.name
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = containerEnv.outputs.registryLoginServer
output AZURE_CONTAINER_REGISTRY_NAME string = containerEnv.outputs.registryName
output API_URL string = containerApp.outputs.url
output APPLICATIONINSIGHTS_CONNECTION_STRING string = monitoring.outputs.appInsightsConnectionString
output SQL_SERVER_FQDN string = sql.outputs.serverFqdn
output SQL_DATABASE_NAME string = sql.outputs.databaseName
output MANAGED_IDENTITY_NAME string = identity.outputs.name
output MANAGED_IDENTITY_CLIENT_ID string = identity.outputs.clientId
output GRAFANA_ENDPOINT string = grafana.outputs.endpoint
output GRAFANA_NAME string = grafana.outputs.grafanaName
output GRAFANA_MCP_ENDPOINT string = grafana.outputs.mcpEndpoint
output AZURE_LOG_ANALYTICS_WORKSPACE_ID string = monitoring.outputs.logAnalyticsWorkspaceId
output CONTAINER_APPS_ENVIRONMENT_NAME string = containerEnv.outputs.environmentName
