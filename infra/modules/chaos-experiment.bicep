targetScope = 'resourceGroup'

param backgroundProcessorName string

resource backgroundProcessorService 'Microsoft.Web/sites@2024-11-01' existing = {
  name: backgroundProcessorName
}

resource backgroundProcessorTarget 'Microsoft.Chaos/targets@2025-01-01' = {
  name: 'microsoft-appservice'
  scope: backgroundProcessorService
  properties: {}
}

resource serviceStopCapability 'Microsoft.Chaos/targets/capabilities@2025-01-01' = {
  parent: backgroundProcessorTarget
  name: 'Stop-1.0'
}

resource experimentStopBackgroundProcessor 'Microsoft.Chaos/experiments@2025-01-01' = {
  name: 'stop-service-background-processor'
  location: resourceGroup().location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    steps: [
      {
        name: 'Step: Stop service BackgroundProcessor'
        branches: [
          {
            name: 'Branch 0'
            actions: [
              {
                name: 'urn:csci:microsoft:appService:stop/1.0'
                type: 'continuous'
                duration: 'PT5M'
                parameters: []
                selectorId: '38ff80b8-076d-4e09-b7c6-27576d4322f3'
              }
            ]
          }
        ]
      }
    ]
    selectors: [
      {
        id: '38ff80b8-076d-4e09-b7c6-27576d4322f3'
        type: 'List'
        targets: [
          {
            id: backgroundProcessorTarget.id
            type: 'ChaosTarget'
          }
        ]
      }
    ]
  }
}

// TODO: Move to RBAC file
var websiteContributorRoleDefinitionResourceId = 'de139f84-1756-47ae-9be6-808fbbe84772'
resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(experimentStopBackgroundProcessor.id, websiteContributorRoleDefinitionResourceId)
  scope: backgroundProcessorService
  properties: {
    principalId: experimentStopBackgroundProcessor.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', websiteContributorRoleDefinitionResourceId)
  }
}
