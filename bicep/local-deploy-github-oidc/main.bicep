targetScope = 'local'

extension az
extension local

param githubRepo {
  owner: string
  name: string
}

param acrResourceGroup {
  subscriptionId: string
  name: string
  location: string
}

resource getAuthToken 'Script' = {
  type: 'bash'
  script: '''
#!/bin/bash
set -e

gh auth token
'''
}

module azure 'azure.bicep' = {
  scope: subscription(acrResourceGroup.subscriptionId)
  params: {
    githubRepo: githubRepo
    acrResourceGroup: acrResourceGroup
  }
}

module ghSecrets 'github.bicep' = {
  params: {
    githubRepo: {
      ...githubRepo
      token: getAuthToken.stdout
    }
    secrets: azure.outputs.secrets
  }
}
