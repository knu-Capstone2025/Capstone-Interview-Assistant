@description('The environment name. Default: dev')
@allowed([
  'dev'
  'test'
  'prod'
])
param environmentName string = 'dev'

@description('The location for all resources. Default: koreacentral')
param location string = resourceGroup().location

@description('The name of the application')
param applicationName string = 'interview-assistant'

@description('The .NET version to use. Default: 9.0')
param dotnetVersion string = '9.0'

@description('The name of the SQL Administrator account')
param sqlAdministratorLogin string

@description('The password for the SQL Administrator account')
@secure()
param sqlAdministratorPassword string

// Configuration for different environments
var isProd = environmentName == 'prod'
var isTest = environmentName == 'test'

// SKU and capacity settings for different environments
var appServicePlanSku = isProd ? 'P1v2' : (isTest ? 'S1' : 'B1')
var sqlDatabaseSku = isProd ? 'Standard' : 'Basic'
var sqlDatabaseTier = isProd ? 'Standard' : 'Basic'
var storageSku = isProd ? 'Standard_GRS' : 'Standard_LRS'
var appServiceCapacity = isProd ? 2 : 1

var resourceNameSuffix = '${applicationName}-${environmentName}'
var appServicePlanName = 'asp-${resourceNameSuffix}'
var webAppName = 'app-${resourceNameSuffix}'
var sqlServerName = 'sql-${resourceNameSuffix}'
var sqlDatabaseName = 'sqldb-${applicationName}'
var appInsightsName = 'ai-${resourceNameSuffix}'
var storageAccountName = replace('st${applicationName}${environmentName}', '-', '')

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: appServicePlanSku
    capacity: appServiceCapacity
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// Web App
resource webApp 'Microsoft.Web/sites@2024-04-01' = {
  name: webAppName
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET|${dotnetVersion}'
      alwaysOn: isProd || isTest
      minTlsVersion: '1.2'
      ftpsState: isProd ? 'Disabled' : 'FtpsOnly'
      appSettings: [
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environmentName == 'prod' ? 'Production' : (environmentName == 'test' ? 'Staging' : 'Development')
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabaseName};Persist Security Info=False;User ID=${sqlAdministratorLogin};Password=${sqlAdministratorPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
        }
        {
          name: 'AzureStorage__ConnectionString'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'Logging__LogLevel__Default'
          value: isProd ? 'Warning' : 'Information'
        }
        {
          name: 'Logging__LogLevel__Microsoft'
          value: isProd ? 'Warning' : 'Information'
        }
      ]
    }
  }
}

// Staging deployment slot (for production and test environments)
resource webAppStagingSlot 'Microsoft.Web/sites/slots@2024-04-01' = if (isProd || isTest) {
  parent: webApp
  name: 'staging'
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET|${dotnetVersion}'
      alwaysOn: true
      appSettings: webApp.properties.siteConfig.appSettings
    }
  }
}

// SQL Server
resource sqlServer 'Microsoft.Sql/servers@2024-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
  }
  
  // Allow Azure services and resources to access this server
  resource firewallRule 'firewallRules' = {
    name: 'AllowAllAzureIPs'
    properties: {
      startIpAddress: '0.0.0.0'
      endIpAddress: '0.0.0.0'
    }
  }
}

// SQL Database
resource sqlDatabase 'Microsoft.Sql/servers/databases@2024-05-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  sku: {
    name: sqlDatabaseSku
    tier: sqlDatabaseTier
  }
  properties: {
    requestedBackupStorageRedundancy: isProd ? 'Geo' : 'Local'
  }
}

// Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    SamplingPercentage: isProd ? 25 : 100
    RetentionInDays: isProd ? 90 : 30
  }
}

// Storage Account
resource storageAccount 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: storageSku
  }
  kind: 'StorageV2'
  properties: {
    accessTier: isProd ? 'Hot' : 'Cool'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
  }
}

// Auto-scaling settings (for production and test environments)
resource webAppAutoScaleSettings 'Microsoft.Insights/autoscalesettings@2022-10-01' = if (isProd || isTest) {
  name: 'autoscale-${webAppName}'
  location: location
  properties: {
    enabled: true
    targetResourceUri: appServicePlan.id
    profiles: [
      {
        name: 'Default'
        capacity: {
          minimum: isProd ? '2' : '1'
          maximum: isProd ? '5' : '3'
          default: isProd ? '2' : '1'
        }
        rules: [
          {
            metricTrigger: {
              metricName: 'CpuPercentage'
              metricResourceUri: appServicePlan.id
              timeGrain: 'PT1M'
              statistic: 'Average'
              timeWindow: 'PT10M'
              timeAggregation: 'Average'
              operator: 'GreaterThan'
              threshold: 70
            }
            scaleAction: {
              direction: 'Increase'
              type: 'ChangeCount'
              value: '1'
              cooldown: 'PT10M'
            }
          }
          {
            metricTrigger: {
              metricName: 'CpuPercentage'
              metricResourceUri: appServicePlan.id
              timeGrain: 'PT1M'
              statistic: 'Average'
              timeWindow: 'PT10M'
              timeAggregation: 'Average'
              operator: 'LessThan'
              threshold: 30
            }
            scaleAction: {
              direction: 'Decrease'
              type: 'ChangeCount'
              value: '1'
              cooldown: 'PT10M'
            }
          }
        ]
      }
    ]
  }
}

// Outputs
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output stagingSlotUrl string = isProd || isTest ? 'https://${webAppStagingSlot.properties.defaultHostName}' : ''
