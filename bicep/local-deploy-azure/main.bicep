targetScope = 'local'

param subscriptionId string
param rgName string

// this will create an actual deployment on Azure
module azure 'azure.bicep' = {
  scope: resourceGroup(subscriptionId, rgName)
}

output armUrl string = azure.outputs.armUrl
