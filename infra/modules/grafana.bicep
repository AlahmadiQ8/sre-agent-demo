// Azure Managed Grafana with Prometheus data source integration
// Grafana's system identity gets Monitoring Reader/Data Reader via RBAC
// Deploying user and app identity get Grafana Admin role

param name string
param location string = resourceGroup().location
param tags object = {}
param monitorWorkspaceId string
param managedIdentityPrincipalId string
param principalId string

var grafanaName = take('graf-${name}', 23)

resource grafana 'Microsoft.Dashboard/grafana@2023-09-01' = {
  name: grafanaName
  location: location
  tags: tags
  sku: {
    name: 'Standard'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    grafanaIntegrations: {
      azureMonitorWorkspaceIntegrations: [
        {
          azureMonitorWorkspaceResourceId: monitorWorkspaceId
        }
      ]
    }
    publicNetworkAccess: 'Enabled'
    apiKey: 'Enabled'
  }
}

// Grafana Admin role for the deploying user
var grafanaAdminRoleId = '22926164-76b3-42b3-bc55-97df8dab3e41'

resource grafanaAdminForUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(principalId)) {
  name: guid(grafana.id, principalId, grafanaAdminRoleId)
  scope: grafana
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', grafanaAdminRoleId)
    principalId: principalId
    principalType: 'User'
  }
}

// Grafana Admin role for the app managed identity
resource grafanaAdminForIdentity 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(grafana.id, managedIdentityPrincipalId, grafanaAdminRoleId)
  scope: grafana
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', grafanaAdminRoleId)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Monitoring Reader — Grafana's system identity can read Azure Monitor metrics
var monitoringReaderRoleId = '43d0d8ad-25c7-4714-9337-8ba259a9fe05'

resource monitoringReaderForGrafana 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, grafana.id, monitoringReaderRoleId)
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', monitoringReaderRoleId)
    principalId: grafana.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Monitoring Data Reader — Grafana's system identity can query the Prometheus workspace
var monitoringDataReaderRoleId = 'b0d8363b-8ddd-447d-831f-62ca05bff136'

resource monitoringDataReaderForGrafana 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(monitorWorkspaceId, grafana.id, monitoringDataReaderRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', monitoringDataReaderRoleId)
    principalId: grafana.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output endpoint string = grafana.properties.endpoint
output grafanaId string = grafana.id
output grafanaName string = grafana.name
output mcpEndpoint string = '${grafana.properties.endpoint}/api/azure-mcp'
