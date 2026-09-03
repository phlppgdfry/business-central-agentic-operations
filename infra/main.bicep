targetScope = 'resourceGroup'

@description('Globally unique lowercase ACR name, 5-50 characters.')
param containerRegistryName string
@description('Short, lowercase deployment name.')
param applicationName string = 'agenticops'
param location string = resourceGroup().location
param apiImage string
param dashboardImage string
@description('Create container apps only after their images are available in ACR.')
param deployApplications bool = true

var logName = '${applicationName}-logs'
var environmentName = '${applicationName}-aca'
var identityName = '${applicationName}-identity'

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logName
  location: location
  properties: { sku: { name: 'PerGB2018' }, retentionInDays: 30 }
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: containerRegistryName
  location: location
  sku: { name: 'Basic' }
  properties: { adminUserEnabled: false, publicNetworkAccess: 'Enabled' }
}

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = { name: identityName, location: location }

resource acrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, identity.id, 'AcrPull')
  scope: registry
  properties: { roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d'), principalId: identity.properties.principalId, principalType: 'ServicePrincipal' }
}

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: environmentName
  location: location
  properties: { appLogsConfiguration: { destination: 'log-analytics', logAnalyticsConfiguration: { customerId: logs.properties.customerId, sharedKey: logs.listKeys().primarySharedKey } } }
}

resource api 'Microsoft.App/containerApps@2024-03-01' = if (deployApplications) {
  name: '${applicationName}-api'
  location: location
  identity: { type: 'UserAssigned', userAssignedIdentities: { '${identity.id}': {} } }
  properties: {
    managedEnvironmentId: environment.id
    configuration: { ingress: { external: false, targetPort: 8080, transport: 'http' }, registries: [{ server: registry.properties.loginServer, identity: identity.id }] }
    template: { containers: [{ name: 'api', image: apiImage, resources: { cpu: json('0.25'), memory: '0.5Gi' } }], scale: { minReplicas: 0, maxReplicas: 2 } }
  }
  dependsOn: [acrPull]
}

resource dashboard 'Microsoft.App/containerApps@2024-03-01' = if (deployApplications) {
  name: '${applicationName}-dashboard'
  location: location
  identity: { type: 'UserAssigned', userAssignedIdentities: { '${identity.id}': {} } }
  properties: {
    managedEnvironmentId: environment.id
    configuration: { ingress: { external: true, targetPort: 8080, transport: 'http' }, registries: [{ server: registry.properties.loginServer, identity: identity.id }] }
    template: { containers: [{ name: 'dashboard', image: dashboardImage, env: [{ name: 'OperationsApi__BaseUrl', value: 'http://${api!.properties.configuration.ingress.fqdn}' }], resources: { cpu: json('0.25'), memory: '0.5Gi' } }], scale: { minReplicas: 0, maxReplicas: 2 } }
  }
  dependsOn: [acrPull]
}

output dashboardUrl string = deployApplications ? 'https://${dashboard!.properties.configuration.ingress.fqdn}' : ''
output registryLoginServer string = registry.properties.loginServer
