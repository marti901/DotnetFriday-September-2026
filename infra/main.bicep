targetScope = 'resourceGroup'

@description('Prefix used for every resource name.')
param namePrefix string = 'monkeybook'

@description('Environment name, for example dev, test or prod.')
@minLength(1)
param environmentName string = 'dev'

@description('Location for all resources except the static web app.')
param location string = resourceGroup().location

@description('Location for the static web app. Static Web Apps is only available in a few regions.')
@allowed([
  'westeurope'
  'northeurope'
  'eastus2'
  'centralus'
  'westus2'
  'eastasia'
])
param staticWebAppLocation string = 'westeurope'

@description('Administrator login for the PostgreSQL flexible server.')
param postgresAdminLogin string = 'monkeyadmin'

@description('Administrator password for the PostgreSQL flexible server.')
@secure()
@minLength(12)
param postgresAdminPassword string

// Suffix that keeps the globally unique names unique per resource group.
var suffix = uniqueString(resourceGroup().id)
var prefix = '${namePrefix}-${environmentName}'

var tags = {
  application: namePrefix
  environment: environmentName
}

var postsApiName = '${prefix}-postsapi-${suffix}'
var feedApiName = '${prefix}-feedapi-${suffix}'
var backgroundProcessorName = '${prefix}-processor-${suffix}'

var databaseName = 'monkeybookdb'
var topicName = 'post-created'
var subscriptionName = 'background-processor'

// Built in role definition ids.
var serviceBusDataSenderRoleId = '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'
var serviceBusDataReceiverRoleId = '4f6d3b9b-027b-4f4c-9142-0e5a3d8880d4'
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

// ---------------------------------------------------------------------------
// Observability
// ---------------------------------------------------------------------------

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${prefix}-logs-${suffix}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${prefix}-appinsights-${suffix}'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

// ---------------------------------------------------------------------------
// Messaging, replaces the Dapr pub/sub that only works locally
// ---------------------------------------------------------------------------

resource serviceBus 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: '${prefix}-sb-${suffix}'
  location: location
  tags: tags
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    disableLocalAuth: true
    minimumTlsVersion: '1.2'
  }
}

resource postCreatedTopic 'Microsoft.ServiceBus/namespaces/topics@2024-01-01' = {
  parent: serviceBus
  name: topicName
  properties: {
    defaultMessageTimeToLive: 'P14D'
  }
}

resource postCreatedSubscription 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2024-01-01' = {
  parent: postCreatedTopic
  name: subscriptionName
  properties: {
    deadLetteringOnMessageExpiration: true
    maxDeliveryCount: 10
    lockDuration: 'PT1M'
  }
}

// ---------------------------------------------------------------------------
// Database
// ---------------------------------------------------------------------------

resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: '${prefix}-pg-${suffix}'
  location: location
  tags: tags
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    version: '16'
    administratorLogin: postgresAdminLogin
    administratorLoginPassword: postgresAdminPassword
    storage: {
      storageSizeGB: 32
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
    network: {
      publicNetworkAccess: 'Enabled'
    }
  }
}

resource monkeyBookDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: postgres
  name: databaseName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// Lets the app services (and any other azure service) reach the server.
resource allowAzureServices 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  parent: postgres
  name: 'AllowAllAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
  dependsOn: [
    monkeyBookDatabase
  ]
}

// ---------------------------------------------------------------------------
// Key vault holding the database connection string
// ---------------------------------------------------------------------------

resource keyVault 'Microsoft.KeyVault/vaults@2024-11-01' = {
  name: 'kv-${namePrefix}-${substring(suffix, 0, 12)}'
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: tenant().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    publicNetworkAccess: 'Enabled'
  }
}

resource databaseConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2024-11-01' = {
  parent: keyVault
  name: 'monkeybookdb-connectionstring'
  properties: {
    value: 'Host=${postgres.properties.fullyQualifiedDomainName};Port=5432;Database=${databaseName};Username=${postgresAdminLogin};Password=${postgresAdminPassword};SSL Mode=Require;Trust Server Certificate=true'
  }
}

// ---------------------------------------------------------------------------
// Compute
// ---------------------------------------------------------------------------

resource appServicePlan 'Microsoft.Web/serverfarms@2024-11-01' = {
  name: '${prefix}-plan'
  location: location
  tags: tags
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

module postsApi 'modules/webapp.bicep' = {
  name: 'postsapi'
  params: {
    name: postsApiName
    location: location
    tags: tags
    appServicePlanId: appServicePlan.id
  }
}

module feedApi 'modules/webapp.bicep' = {
  name: 'feedapi'
  params: {
    name: feedApiName
    location: location
    tags: tags
    appServicePlanId: appServicePlan.id
  }
}

module backgroundProcessor 'modules/webapp.bicep' = {
  name: 'backgroundprocessor'
  params: {
    name: backgroundProcessorName
    location: location
    tags: tags
    appServicePlanId: appServicePlan.id
  }
}

// ---------------------------------------------------------------------------
// Frontend
// ---------------------------------------------------------------------------

resource staticWebApp 'Microsoft.Web/staticSites@2024-11-01' = {
  name: '${prefix}-frontend-${suffix}'
  location: staticWebAppLocation
  tags: tags
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    // The site is uploaded by the deployment script or the workflow, not from a repository.
    allowConfigFileUpdates: true
  }
}

var frontendUrl = 'https://${staticWebApp.properties.defaultHostname}'

// ---------------------------------------------------------------------------
// Access, every app uses its own system assigned identity
// ---------------------------------------------------------------------------

resource postsApiCanSend 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: serviceBus
  name: guid(serviceBus.id, postsApiName, serviceBusDataSenderRoleId)
  properties: {
    principalId: postsApi.outputs.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      serviceBusDataSenderRoleId
    )
  }
}

resource processorCanReceive 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: serviceBus
  name: guid(serviceBus.id, backgroundProcessorName, serviceBusDataReceiverRoleId)
  properties: {
    principalId: backgroundProcessor.outputs.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      serviceBusDataReceiverRoleId
    )
  }
}

resource feedApiCanReadSecrets 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, feedApiName, keyVaultSecretsUserRoleId)
  properties: {
    principalId: feedApi.outputs.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
  }
}

resource processorCanReadSecrets 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, backgroundProcessorName, keyVaultSecretsUserRoleId)
  properties: {
    principalId: backgroundProcessor.outputs.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
  }
}

// ---------------------------------------------------------------------------
// App settings, applied after the role assignments so the key vault references resolve
// ---------------------------------------------------------------------------

var databaseConnectionReference = '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=${databaseConnectionSecret.name})'

var sharedSettings = {
  ASPNETCORE_ENVIRONMENT: 'Production'
  APPLICATIONINSIGHTS_CONNECTION_STRING: appInsights.properties.ConnectionString
}

resource postsApiSettings 'Microsoft.Web/sites/config@2024-11-01' = {
  name: '${postsApiName}/appsettings'
  properties: union(sharedSettings, {
    ServiceBus__Namespace: '${serviceBus.name}.servicebus.windows.net'
    ServiceBus__Topic: topicName
    Cors__AllowedOrigins__0: frontendUrl
  })
  dependsOn: [
    postsApiCanSend
  ]
}

resource feedApiSettings 'Microsoft.Web/sites/config@2024-11-01' = {
  name: '${feedApiName}/appsettings'
  properties: union(sharedSettings, {
    ConnectionStrings__monkeybookdb: databaseConnectionReference
    Cors__AllowedOrigins__0: frontendUrl
  })
  dependsOn: [
    feedApiCanReadSecrets
  ]
}

resource backgroundProcessorSettings 'Microsoft.Web/sites/config@2024-11-01' = {
  name: '${backgroundProcessorName}/appsettings'
  properties: union(sharedSettings, {
    ConnectionStrings__monkeybookdb: databaseConnectionReference
    ServiceBus__Namespace: '${serviceBus.name}.servicebus.windows.net'
    ServiceBus__Topic: topicName
    ServiceBus__Subscription: subscriptionName
  })
  dependsOn: [
    processorCanReceive
    processorCanReadSecrets
    postCreatedSubscription
  ]
}

// ---------------------------------------------------------------------------
// Outputs, used by deploy.ps1 and by the workflow
// ---------------------------------------------------------------------------

output resourceGroupName string = resourceGroup().name
output postsApiName string = postsApiName
output postsApiUrl string = postsApi.outputs.url
output feedApiName string = feedApiName
output feedApiUrl string = feedApi.outputs.url
output backgroundProcessorName string = backgroundProcessorName
output backgroundProcessorUrl string = backgroundProcessor.outputs.url
output staticWebAppName string = staticWebApp.name
output frontendUrl string = frontendUrl
output serviceBusNamespace string = '${serviceBus.name}.servicebus.windows.net'
output keyVaultName string = keyVault.name
output postgresFqdn string = postgres.properties.fullyQualifiedDomainName
