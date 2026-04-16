@description('Base name for monitoring resources')
param baseName string

@description('Azure region for resources')
param location string

@description('Tags to apply to all resources')
param tags object = {}

var logAnalyticsName = 'log-${baseName}'
var appInsightsName = 'appi-${baseName}'

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

@description('Application Insights instrumentation key')
output instrumentationKey string = applicationInsights.properties.InstrumentationKey

@description('Application Insights connection string')
output connectionString string = applicationInsights.properties.ConnectionString

@description('Application Insights resource ID')
output applicationInsightsId string = applicationInsights.id

@description('Log Analytics workspace ID')
output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.id
