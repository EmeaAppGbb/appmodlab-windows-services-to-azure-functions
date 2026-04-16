@description('Environment name used for resource naming')
@allowed([
  'dev'
  'staging'
  'prod'
])
param environmentName string = 'dev'

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('SQL Server administrator login')
param sqlAdminLogin string

@secure()
@description('SQL Server administrator password')
param sqlAdminPassword string

@description('Azure Communication Services connection string')
param communicationServicesConnectionString string = ''

@description('Sender email address for Azure Communication Services')
param communicationServicesSenderAddress string = 'DoNotReply@meridian.com'

@description('Email address for compliance notifications')
param complianceNotificationEmail string = 'compliance@meridian.com'

@description('Daily return threshold for portfolio valuation alerts')
param dailyReturnThreshold string = '0.05'

@description('SQL Database SKU name')
@allowed([
  'Basic'
  'S0'
  'S1'
  'S2'
  'GP_S_Gen5_1'
])
param sqlSkuName string = 'S0'

var baseName = 'meridian-${environmentName}'

var tags = {
  environment: environmentName
  application: 'meridian-capital'
  managedBy: 'bicep'
}

// Monitoring (Log Analytics + Application Insights)
module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring-${environmentName}'
  params: {
    baseName: baseName
    location: location
    tags: tags
  }
}

// Storage Account (blobs + queues)
module storage 'modules/storage.bicep' = {
  name: 'storage-${environmentName}'
  params: {
    baseName: baseName
    location: location
    tags: tags
  }
}

// Azure SQL Database
module sqlDatabase 'modules/sql-database.bicep' = {
  name: 'sql-${environmentName}'
  params: {
    baseName: baseName
    location: location
    tags: tags
    sqlAdminLogin: sqlAdminLogin
    sqlAdminPassword: sqlAdminPassword
    sqlSkuName: sqlSkuName
  }
}

// Azure Communication Services
resource communicationServices 'Microsoft.Communication/communicationServices@2023-04-01' = {
  name: 'acs-${baseName}'
  location: 'global'
  tags: tags
  properties: {
    dataLocation: 'United States'
  }
}

// Function App
module functionApp 'modules/function-app.bicep' = {
  name: 'function-app-${environmentName}'
  params: {
    baseName: baseName
    location: location
    tags: tags
    storageConnectionString: storage.outputs.connectionString
    sqlConnectionString: sqlDatabase.outputs.connectionString
    appInsightsInstrumentationKey: monitoring.outputs.instrumentationKey
    appInsightsConnectionString: monitoring.outputs.connectionString
    communicationServicesConnectionString: communicationServicesConnectionString
    communicationServicesSenderAddress: communicationServicesSenderAddress
    complianceNotificationEmail: complianceNotificationEmail
    dailyReturnThreshold: dailyReturnThreshold
  }
}

// Outputs
@description('Function App name')
output functionAppName string = functionApp.outputs.functionAppName

@description('Function App URL')
output functionAppUrl string = 'https://${functionApp.outputs.defaultHostName}'

@description('Storage Account name')
output storageAccountName string = storage.outputs.storageAccountName

@description('SQL Server FQDN')
output sqlServerFqdn string = sqlDatabase.outputs.sqlServerFqdn

@description('Application Insights resource ID')
output applicationInsightsId string = monitoring.outputs.applicationInsightsId

@description('Communication Services name')
output communicationServicesName string = communicationServices.name
