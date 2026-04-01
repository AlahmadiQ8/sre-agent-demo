// Azure Monitor Workspace for Grafana metrics integration

param name string
param location string = resourceGroup().location
param tags object = {}

resource monitorWorkspace 'Microsoft.Monitor/accounts@2023-04-03' = {
  name: 'amw-${name}'
  location: location
  tags: tags
}

output monitorWorkspaceId string = monitorWorkspace.id
output monitorWorkspaceName string = monitorWorkspace.name
