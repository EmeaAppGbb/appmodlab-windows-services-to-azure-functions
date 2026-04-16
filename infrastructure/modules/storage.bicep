@description('Base name for storage resources')
param baseName string

@description('Azure region for resources')
param location string

@description('Tags to apply to all resources')
param tags object = {}

// Storage account names must be 3-24 chars, lowercase alphanumeric only
var storageAccountName = 'st${replace(baseName, '-', '')}' 

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: length(storageAccountName) > 24 ? substring(storageAccountName, 0, 24) : storageAccountName
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    accessTier: 'Hot'
  }
}

resource blobServices 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource queueServices 'Microsoft.Storage/storageAccounts/queueServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

// Blob containers
resource incomingDocuments 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobServices
  name: 'incoming-documents'
  properties: {
    publicAccess: 'None'
  }
}

resource processedDocuments 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobServices
  name: 'processed-documents'
  properties: {
    publicAccess: 'None'
  }
}

resource valuationResults 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobServices
  name: 'valuation-results'
  properties: {
    publicAccess: 'None'
  }
}

resource marketData 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobServices
  name: 'market-data'
  properties: {
    publicAccess: 'None'
  }
}

resource complianceReports 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobServices
  name: 'compliance-reports'
  properties: {
    publicAccess: 'None'
  }
}

// Queues
resource extractionQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = {
  parent: queueServices
  name: 'extraction-queue'
}

resource complianceQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = {
  parent: queueServices
  name: 'compliance-queue'
}

resource notificationQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = {
  parent: queueServices
  name: 'notification-queue'
}

resource alertQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = {
  parent: queueServices
  name: 'alert-queue'
}

resource documentClassificationQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = {
  parent: queueServices
  name: 'document-classification'
}

@description('Storage account name')
output storageAccountName string = storageAccount.name

@description('Storage account connection string')
output connectionString string = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'

@description('Storage account resource ID')
output storageAccountId string = storageAccount.id
