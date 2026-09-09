// A single Linux .NET web app with a system assigned identity.
// The app settings are set separately so they can wait for the rbac assignments.

param name string
param location string
param tags object
param appServicePlanId string

resource site 'Microsoft.Web/sites@2024-11-01' = {
  name: name
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
    }
  }
}

output id string = site.id
output name string = site.name
output principalId string = site.identity.principalId
output url string = 'https://${site.properties.defaultHostName}'
