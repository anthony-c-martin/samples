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

resource repo 'Repository' = {
  owner: githubRepo.owner
  name: githubRepo.name
  visibility: 'Public'
}

resource tenantId 'ActionsSecret' = {
  owner: repo.owner
  repo: repo.name
  name: 'AZURE_TENANT_ID'
  #disable-next-line use-secure-value-for-secure-inputs
  value: secrets.tenantId
}

resource subscriptionId 'ActionsSecret' = {
  owner: repo.owner
  repo: repo.name
  name: 'AZURE_SUBSCRIPTION_ID'
  #disable-next-line use-secure-value-for-secure-inputs
  value: secrets.subscriptionId
}

resource clientId 'ActionsSecret' = {
  owner: repo.owner
  repo: repo.name
  name: 'AZURE_CLIENT_ID'
  #disable-next-line use-secure-value-for-secure-inputs
  value: secrets.clientId
}
