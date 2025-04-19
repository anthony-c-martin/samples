using 'main.bicep'

param acrResourceGroup = {
  name: 'bicepextdemo'
  location: 'East US 2'
  subscriptionId: 'd08e1a72-8180-4ed3-8125-9dff7376b0bd'
}

param githubRepo = {
  name: 'anthony-c-martin'
  owner: 'test-repo'
}
