@description('Base name for SQL resources')
param baseName string

@description('Azure region for resources')
param location string

@description('Tags to apply to all resources')
param tags object = {}

@description('SQL Server administrator login')
param sqlAdminLogin string

@secure()
@description('SQL Server administrator password')
param sqlAdminPassword string

@description('SQL Database SKU name')
@allowed([
  'Basic'
  'S0'
  'S1'
  'S2'
  'GP_S_Gen5_1'
])
param sqlSkuName string = 'S0'

var sqlServerName = 'sql-${baseName}'
var sqlDatabaseName = 'sqldb-${baseName}'

resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// Allow Azure services to access the SQL Server
resource firewallRule 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2021-11-01' = {
  name: '${sqlServerName}/${sqlDatabaseName}'
  location: location
  tags: tags
  sku: {
    name: sqlSkuName
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648 // 2 GB
  }
  dependsOn: [
    sqlServer
  ]
}

@description('SQL Server fully qualified domain name')
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('SQL Database name')
output databaseName string = sqlDatabase.name

@description('SQL connection string')
output connectionString string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabase.name};Persist Security Info=False;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

@description('SQL Server resource ID')
output sqlServerId string = sqlServer.id
