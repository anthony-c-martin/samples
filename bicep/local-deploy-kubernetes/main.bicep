targetScope = 'local'

extension az
extension local

resource getKubeConfig 'Script' = {
  type: 'bash'
  script: 'kubectl config view --raw'
}

module aksStoreApp 'aks-store.bicep' = {
  params: {
    kubeConfig: base64(getKubeConfig.stdout)
  }
}
