@description('Base name for Function App resources')
param baseName string

@description('Azure region for resources')
param location string

@description('Tags to apply to all resources')
param tags object = {}

@description('Storage account connection string for AzureWebJobsStorage')
param storageConnectionString string

@description('SQL Database connection string')
param sqlConnectionString string

@description('Application Insights instrumentation key')
param appInsightsInstrumentationKey string

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Azure Communication Services connection string')
param communicationServicesConnectionString string

@description('Sender email address for Azure Communication Services')
param communicationServicesSenderAddress string = 'DoNotReply@meridian.com'

@description('Email address for compliance notifications')
param complianceNotificationEmail string = 'compliance@meridian.com'

@description('Daily return threshold for portfolio valuation alerts')
param dailyReturnThreshold string = '0.05'

var functionAppName = 'func-${baseName}'
var hostingPlanName = 'plan-${baseName}'

resource hostingPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: hostingPlanName
  location: location
  tags: tags
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {}
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  tags: union(tags, { 'azd-service-name': 'meridian-functions' })
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    httpsOnly: true
    siteConfig: {
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      netFrameworkVersion: 'v9.0'
      use32BitWorkerProcess: false
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: storageConnectionString
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: storageConnectionString
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(functionAppName)
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsightsInstrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'SqlConnectionString'
          value: sqlConnectionString
        }
        {
          name: 'AzureCommunicationServicesConnectionString'
          value: communicationServicesConnectionString
        }
        {
          name: 'AzureCommunicationServicesSenderAddress'
          value: communicationServicesSenderAddress
        }
        {
          name: 'ComplianceNotificationEmail'
          value: complianceNotificationEmail
        }
        {
          name: 'DailyReturnThreshold'
          value: dailyReturnThreshold
        }
      ]
    }
  }
}

@description('Function App name')
output functionAppName string = functionApp.name

@description('Function App default hostname')
output defaultHostName string = functionApp.properties.defaultHostName

@description('Function App resource ID')
output functionAppId string = functionApp.id

@description('Function App managed identity principal ID')
output principalId string = functionApp.identity.principalId
