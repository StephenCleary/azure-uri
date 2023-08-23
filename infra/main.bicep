targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the the environment which is used to generate a short unique hash used in all resources.')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

param principalId string

var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = { 'azd-env-name': environmentName }
var resourceGroupName = '${abbrs.resourcesResourceGroups}${environmentName}'
var apiServiceName = '${abbrs.webSitesFunctions}api-${resourceToken}'
var appServicePlanName = '${abbrs.webServerFarms}${resourceToken}'
var storageAccountName = '${abbrs.storageStorageAccounts}${resourceToken}'
var cosmosAccountName = '${abbrs.documentDBDatabaseAccounts}${resourceToken}'
var cosmosDatabaseName = '${abbrs.documentDBDatabases}${resourceToken}'
var logAnalyticsName = '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
var applicationInsightsName = '${abbrs.insightsComponents}${resourceToken}'
var applicationInsightsDashboardName = '${abbrs.portalDashboards}${resourceToken}'

// Organize resources in a resource group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

module api './app/api.bicep' = {
  name: 'api'
  scope: rg
  params: {
    name: apiServiceName
    location: location
    tags: tags
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    appServicePlanId: appServicePlan.outputs.id
    storageAccountName: storage.outputs.name
    appSettings: {
      AZURE_COSMOSDB_ENDPOINT: cosmos.outputs.endpoint
      AZURE_COSMOSDB_DATABASE_NAME: cosmos.outputs.databaseName
    }
  }
}

// Create an App Service Plan to group applications under the same payment plan and SKU
module appServicePlan './core/host/appserviceplan.bicep' = {
  name: 'appserviceplan'
  scope: rg
  params: {
    name: appServicePlanName
    location: location
    tags: tags
    sku: {
      name: 'Y1'
      tier: 'Dynamic'
    }
  }
}

// Backing storage for Azure functions backend API
module storage './core/storage/storage-account.bicep' = {
  name: 'storage'
  scope: rg
  params: {
    name: storageAccountName
    location: location
    tags: tags
  }
}

module cosmos './core/database/cosmos/sql/cosmos-sql-db.bicep' = {
  name: 'cosmos'
  scope: rg
  params: {
    accountName: cosmosAccountName
    databaseName: cosmosDatabaseName
    location: location
    containers: [
      { id: 'slugs', name: 'slugs', partitionKey: '/slug' }
      { id: 'logs', name: 'logs', partitionKey: '/slug' }
    ]
  }
}

// Point-read-only role.
module proRoleDefinition './core/database/cosmos/sql/cosmos-sql-role-def.bicep' = {
  name: 'cosmos-sql-role-definition-pro'
  scope: rg
  params: {
    accountName: cosmosAccountName
  }
  dependsOn: [
    cosmos
  ]
}

// API has point-read-only access to `slugs`
module apiSlugsRoleAssignment './core/database/cosmos/sql/cosmos-sql-role-assign.bicep' = {
  name: 'cosmos-sql-api-role-slugs'
  scope: rg
  params: {
    accountName: cosmosAccountName
    roleDefinitionId: proRoleDefinition.outputs.id
    principalId: api.outputs.SERVICE_API_IDENTITY_PRINCIPAL_ID
    databaseName: cosmosDatabaseName
    containerName: 'slugs'
  }
}

// API has read/write access to `logs`
module apiLogsRoleAssignment './core/database/cosmos/sql/cosmos-sql-role-assign.bicep' = {
  name: 'cosmos-sql-api-role-logs'
  scope: rg
  params: {
    accountName: cosmosAccountName
    roleDefinitionId: '${cosmos.outputs.accountId}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'
    principalId: api.outputs.SERVICE_API_IDENTITY_PRINCIPAL_ID
    databaseName: cosmosDatabaseName
    containerName: 'logs'
  }
}

// CLI has read/write access to entire database
module userRoleAssignment './core/database/cosmos/sql/cosmos-sql-role-assign.bicep' = {
  name: 'cosmos-sql-user-role'
  scope: rg
  params: {
    accountName: cosmosAccountName
    roleDefinitionId: '${cosmos.outputs.accountId}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'
    principalId: principalId
    databaseName: cosmosDatabaseName
  }
  dependsOn: [
    cosmos
  ]
}

// Monitor application with Azure Monitor
module monitoring './core/monitor/monitoring.bicep' = {
  name: 'monitoring'
  scope: rg
  params: {
    location: location
    tags: tags
    logAnalyticsName: logAnalyticsName
    applicationInsightsName: applicationInsightsName
    applicationInsightsDashboardName: applicationInsightsDashboardName
  }
}

output APPLICATIONINSIGHTS_CONNECTION_STRING string = monitoring.outputs.applicationInsightsConnectionString
output AZURE_COSMOSDB_ENDPOINT string = cosmos.outputs.endpoint
output AZURE_COSMOSDB_DATABASE_NAME string = cosmosDatabaseName
output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
