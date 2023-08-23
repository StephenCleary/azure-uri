param accountName string
param roleDefinitionId string
param principalId string
param databaseName string
param containerName string = ''

var scope = empty(containerName) ? '${cosmos.id}/dbs/${databaseName}' : '${cosmos.id}/dbs/${databaseName}/colls/${containerName}'

resource role 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2022-05-15' = {
  parent: cosmos
  name: guid(roleDefinitionId, principalId, cosmos.id, databaseName, containerName)
  properties: {
    principalId: principalId
    roleDefinitionId: roleDefinitionId
    scope: scope
  }
}

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2022-08-15' existing = {
  name: accountName
}
