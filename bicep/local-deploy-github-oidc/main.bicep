targetScope = 'local'

import { GitHubRepoConfig } from './types.bicep'

extension az
extension local

param gitHubRepo GitHubRepoConfig

param acrResourceGroup {
  subscriptionId: string
  name: string
  location: string
}

resource getAuthToken 'Script' = {
  type: 'bash'
  script: 'gh auth token'
}

module azure 'azure.bicep' = {
  scope: subscription(acrResourceGroup.subscriptionId)
  params: {
    gitHubRepo: gitHubRepo
    acrResourceGroup: acrResourceGroup
  }
}

module ghSecrets 'github.bicep' = {
  params: {
    gitHubToken: trim(getAuthToken.stdout)
    gitHubRepo: gitHubRepo
    azureOidcConfig: azure.outputs.oidcConfig
  }
}
