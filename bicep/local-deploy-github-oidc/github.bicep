targetScope = 'local'

extension github with {
  token: githubRepo.token
}

param githubRepo {
  owner: string
  name: string
  @secure()
  token: string
}

@secure()
param secrets {
  tenantId: string
  subscriptionId: string
  clientId: string
}

resource tenantId 'ActionsSecret' = {
  owner: githubRepo.owner
  repo: githubRepo.name
  name: 'AZURE_TENANT_ID'
  value: secrets.tenantId
}

resource subscriptionId 'ActionsSecret' = {
  owner: githubRepo.owner
  repo: githubRepo.name
  name: 'AZURE_SUBSCRIPTION_ID'
  value: secrets.subscriptionId
}

resource clientId 'ActionsSecret' = {
  owner: githubRepo.owner
  repo: githubRepo.name
  name: 'AZURE_CLIENT_ID'
  value: secrets.clientId
}
