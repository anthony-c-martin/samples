targetScope = 'subscription'

extension msGraph
extension az

param githubRepo {
  owner: string
  name: string
}

param acrResourceGroup {
  name: string
  location: string
}

resource githubApp 'Microsoft.Graph/applications@v1.0' = {
  displayName: githubRepo.name
  uniqueName: githubRepo.name

  resource childSymbolicname 'federatedIdentityCredentials@v1.0' = {
    name: '${githubApp.uniqueName}/${githubRepo.name}'
    audiences: ['api://AzureADTokenExchange']
    issuer: 'https://token.actions.githubusercontent.com'
    subject: 'repo:${githubRepo.owner}/${githubRepo.name}:ref:refs/heads/main'
    description: 'GitHub OIDC Connection for ${githubRepo.owner}/${githubRepo.name}'
  }
}

resource githubSp 'Microsoft.Graph/servicePrincipals@v1.0' = {
  appId: githubApp.appId
}

resource ownerRoleDefinition 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: tenant()
  name: '8e3af657-a8ff-443c-a75c-2fe8c4bcb635'
}

resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: acrResourceGroup.name
  location: acrResourceGroup.location
}

resource ownerRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(githubRepo.name, ownerRoleDefinition.id, resourceGroup.id)
  properties: {
    principalId: githubSp.id
    roleDefinitionId: ownerRoleDefinition.id
    principalType: 'ServicePrincipal'
  }
}

output secrets {
  tenantId: string
  subscriptionId: string
  clientId: string
} = {
  tenantId: subscription().tenantId
  subscriptionId: subscription().subscriptionId
  clientId: githubApp.appId
}
