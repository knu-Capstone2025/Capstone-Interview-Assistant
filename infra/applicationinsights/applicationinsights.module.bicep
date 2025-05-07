@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param applicationType string = 'web'

param kind string = 'web'

param logAnalyticsWorkspaceId string

param tags object = {
  'aspire-resource-name': 'applicationinsights'
}

var resourceToken = uniqueString(resourceGroup().id)
var name = 'applicationinsights-${resourceToken}'

resource applicationinsights 'Microsoft.Insights/components@2020-02-02' = {
  name: name
  kind: kind
  location: location
  properties: {
    Application_Type: applicationType
    WorkspaceResourceId: logAnalyticsWorkspaceId
    DisableLocalAuth: false
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    RetentionInDays: 90
  }
  tags: tags
}

output appInsightsConnectionString string = applicationinsights.properties.ConnectionString
output appInsightsInstrumentationKey string = applicationinsights.properties.InstrumentationKey
output appInsightsName string = applicationinsights.name
output appInsightsId string = applicationinsights.id
