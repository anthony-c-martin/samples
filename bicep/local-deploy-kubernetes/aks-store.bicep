targetScope = 'local'

@secure()
param kubeConfig string

extension kubernetes with {
  namespace: 'default'
  kubeConfig: kubeConfig
}

resource appsStatefulSet_mongodb 'apps/StatefulSet@v1' = {
  metadata: {
    name: 'mongodb'
  }
  spec: {
    serviceName: 'mongodb'
    replicas: 1
    selector: {
      matchLabels: {
        app: 'mongodb'
      }
    }
    template: {
      metadata: {
        labels: {
          app: 'mongodb'
        }
      }
      spec: {
        nodeSelector: {
          'kubernetes.io/os': 'linux'
        }
        containers: [
          {
            name: 'mongodb'
            image: 'mcr.microsoft.com/mirror/docker/library/mongo:4.2'
            ports: [
              {
                containerPort: any(27017)
                name: 'mongodb'
              }
            ]
            resources: {
              requests: {
                cpu: '5m'
                memory: '75Mi'
              }
              limits: {
                cpu: '25m'
                memory: '1024Mi'
              }
            }
            livenessProbe: {
              exec: {
                command: [
                  'mongosh'
                  '--eval'
                  'db.runCommand(\'ping\').ok'
                ]
              }
              initialDelaySeconds: 5
              periodSeconds: 5
            }
          }
        ]
      }
    }
  }
}

resource coreService_mongodb 'core/Service@v1' = {
  metadata: {
    name: 'mongodb'
  }
  spec: {
    ports: [
      {
        port: any(27017)
      }
    ]
    selector: {
      app: 'mongodb'
    }
    type: 'ClusterIP'
  }
}

resource coreConfigMap_rabbitmqEnabledPlugins 'core/ConfigMap@v1' = {
  data: {
    rabbitmq_enabled_plugins: '[rabbitmq_management,rabbitmq_prometheus,rabbitmq_amqp1_0].\n'
  }
  metadata: {
    name: 'rabbitmq-enabled-plugins'
  }
}

resource appsStatefulSet_rabbitmq 'apps/StatefulSet@v1' = {
  metadata: {
    name: 'rabbitmq'
  }
  spec: {
    serviceName: 'rabbitmq'
    replicas: 1
    selector: {
      matchLabels: {
        app: 'rabbitmq'
      }
    }
    template: {
      metadata: {
        labels: {
          app: 'rabbitmq'
        }
      }
      spec: {
        nodeSelector: {
          'kubernetes.io/os': 'linux'
        }
        containers: [
          {
            name: 'rabbitmq'
            image: 'mcr.microsoft.com/mirror/docker/library/rabbitmq:3.10-management-alpine'
            ports: [
              {
                containerPort: any(5672)
                name: 'rabbitmq-amqp'
              }
              {
                containerPort: any(15672)
                name: 'rabbitmq-http'
              }
            ]
            env: [
              {
                name: 'RABBITMQ_DEFAULT_USER'
                value: 'username'
              }
              {
                name: 'RABBITMQ_DEFAULT_PASS'
                value: 'password'
              }
            ]
            resources: {
              requests: {
                cpu: '10m'
                memory: '128Mi'
              }
              limits: {
                cpu: '250m'
                memory: '256Mi'
              }
            }
            volumeMounts: [
              {
                name: 'rabbitmq-enabled-plugins'
                mountPath: '/etc/rabbitmq/enabled_plugins'
                subPath: 'enabled_plugins'
              }
            ]
          }
        ]
        volumes: [
          {
            name: 'rabbitmq-enabled-plugins'
            configMap: {
              name: 'rabbitmq-enabled-plugins'
              items: [
                {
                  key: 'rabbitmq_enabled_plugins'
                  path: 'enabled_plugins'
                }
              ]
            }
          }
        ]
      }
    }
  }
}

resource coreService_rabbitmq 'core/Service@v1' = {
  metadata: {
    name: 'rabbitmq'
  }
  spec: {
    selector: {
      app: 'rabbitmq'
    }
    ports: [
      {
        name: 'rabbitmq-amqp'
        port: any(5672)
        targetPort: any(5672)
      }
      {
        name: 'rabbitmq-http'
        port: any(15672)
        targetPort: any(15672)
      }
    ]
    type: 'ClusterIP'
  }
}

resource appsDeployment_orderService 'apps/Deployment@v1' = {
  metadata: {
    name: 'order-service'
  }
  spec: {
    replicas: 1
    selector: {
      matchLabels: {
        app: 'order-service'
      }
    }
    template: {
      metadata: {
        labels: {
          app: 'order-service'
        }
      }
      spec: {
        nodeSelector: {
          'kubernetes.io/os': 'linux'
        }
        containers: [
          {
            name: 'order-service'
            image: 'ghcr.io/azure-samples/aks-store-demo/order-service:latest'
            ports: [
              {
                containerPort: any(3000)
              }
            ]
            env: [
              {
                name: 'ORDER_QUEUE_HOSTNAME'
                value: 'rabbitmq'
              }
              {
                name: 'ORDER_QUEUE_PORT'
                value: '5672'
              }
              {
                name: 'ORDER_QUEUE_USERNAME'
                value: 'username'
              }
              {
                name: 'ORDER_QUEUE_PASSWORD'
                value: 'password'
              }
              {
                name: 'ORDER_QUEUE_NAME'
                value: 'orders'
              }
              {
                name: 'FASTIFY_ADDRESS'
                value: '0.0.0.0'
              }
            ]
            resources: {
              requests: {
                cpu: '1m'
                memory: '50Mi'
              }
              limits: {
                cpu: '100m'
                memory: '256Mi'
              }
            }
            startupProbe: {
              httpGet: {
                path: '/health'
                port: any(3000)
              }
              failureThreshold: 5
              initialDelaySeconds: 20
              periodSeconds: 10
            }
            readinessProbe: {
              httpGet: {
                path: '/health'
                port: any(3000)
              }
              failureThreshold: 3
              initialDelaySeconds: 3
              periodSeconds: 5
            }
            livenessProbe: {
              httpGet: {
                path: '/health'
                port: any(3000)
              }
              failureThreshold: 5
              initialDelaySeconds: 3
              periodSeconds: 3
            }
          }
        ]
        initContainers: [
          {
            name: 'wait-for-rabbitmq'
            image: 'busybox'
            command: [
              'sh'
              '-c'
              'until nc -zv rabbitmq 5672; do echo waiting for rabbitmq; sleep 2; done;'
            ]
            resources: {
              requests: {
                cpu: '1m'
                memory: '50Mi'
              }
              limits: {
                cpu: '100m'
                memory: '256Mi'
              }
            }
          }
        ]
      }
    }
  }
}

resource coreService_orderService 'core/Service@v1' = {
  metadata: {
    name: 'order-service'
  }
  spec: {
    type: 'ClusterIP'
    ports: [
      {
        name: 'http'
        port: any(3000)
        targetPort: any(3000)
      }
    ]
    selector: {
      app: 'order-service'
    }
  }
}

resource appsDeployment_makelineService 'apps/Deployment@v1' = {
  metadata: {
    name: 'makeline-service'
  }
  spec: {
    replicas: 1
    selector: {
      matchLabels: {
        app: 'makeline-service'
      }
    }
    template: {
      metadata: {
        labels: {
          app: 'makeline-service'
        }
      }
      spec: {
        nodeSelector: {
          'kubernetes.io/os': 'linux'
        }
        containers: [
          {
            name: 'makeline-service'
            image: 'ghcr.io/azure-samples/aks-store-demo/makeline-service:latest'
            ports: [
              {
                containerPort: any(3001)
              }
            ]
            env: [
              {
                name: 'ORDER_QUEUE_URI'
                value: 'amqp://rabbitmq:5672'
              }
              {
                name: 'ORDER_QUEUE_USERNAME'
                value: 'username'
              }
              {
                name: 'ORDER_QUEUE_PASSWORD'
                value: 'password'
              }
              {
                name: 'ORDER_QUEUE_NAME'
                value: 'orders'
              }
              {
                name: 'ORDER_DB_URI'
                value: 'mongodb://mongodb:27017'
              }
              {
                name: 'ORDER_DB_NAME'
                value: 'orderdb'
              }
              {
                name: 'ORDER_DB_COLLECTION_NAME'
                value: 'orders'
              }
            ]
            resources: {
              requests: {
                cpu: '1m'
                memory: '6Mi'
              }
              limits: {
                cpu: '5m'
                memory: '20Mi'
              }
            }
            startupProbe: {
              httpGet: {
                path: '/health'
                port: any(3001)
              }
              failureThreshold: 10
              periodSeconds: 5
            }
            readinessProbe: {
              httpGet: {
                path: '/health'
                port: any(3001)
              }
              failureThreshold: 3
              initialDelaySeconds: 3
              periodSeconds: 5
            }
            livenessProbe: {
              httpGet: {
                path: '/health'
                port: any(3001)
              }
              failureThreshold: 5
              initialDelaySeconds: 3
              periodSeconds: 3
            }
          }
        ]
      }
    }
  }
}

resource coreService_makelineService 'core/Service@v1' = {
  metadata: {
    name: 'makeline-service'
  }
  spec: {
    type: 'ClusterIP'
    ports: [
      {
        name: 'http'
        port: any(3001)
        targetPort: any(3001)
      }
    ]
    selector: {
      app: 'makeline-service'
    }
  }
}

resource appsDeployment_productService 'apps/Deployment@v1' = {
  metadata: {
    name: 'product-service'
  }
  spec: {
    replicas: 1
    selector: {
      matchLabels: {
        app: 'product-service'
      }
    }
    template: {
      metadata: {
        labels: {
          app: 'product-service'
        }
      }
      spec: {
        nodeSelector: {
          'kubernetes.io/os': 'linux'
        }
        containers: [
          {
            name: 'product-service'
            image: 'ghcr.io/azure-samples/aks-store-demo/product-service:latest'
            ports: [
              {
                containerPort: any(3002)
              }
            ]
            env: [
              {
                name: 'AI_SERVICE_URL'
                value: 'http://ai-service:5001/'
              }
            ]
            resources: {
              requests: {
                cpu: '1m'
                memory: '1Mi'
              }
              limits: {
                cpu: '2m'
                memory: '20Mi'
              }
            }
            readinessProbe: {
              httpGet: {
                path: '/health'
                port: any(3002)
              }
              failureThreshold: 3
              initialDelaySeconds: 3
              periodSeconds: 5
            }
            livenessProbe: {
              httpGet: {
                path: '/health'
                port: any(3002)
              }
              failureThreshold: 5
              initialDelaySeconds: 3
              periodSeconds: 3
            }
          }
        ]
      }
    }
  }
}

resource coreService_productService 'core/Service@v1' = {
  metadata: {
    name: 'product-service'
  }
  spec: {
    type: 'ClusterIP'
    ports: [
      {
        name: 'http'
        port: any(3002)
        targetPort: any(3002)
      }
    ]
    selector: {
      app: 'product-service'
    }
  }
}

resource appsDeployment_storeFront 'apps/Deployment@v1' = {
  metadata: {
    name: 'store-front'
  }
  spec: {
    replicas: 1
    selector: {
      matchLabels: {
        app: 'store-front'
      }
    }
    template: {
      metadata: {
        labels: {
          app: 'store-front'
        }
      }
      spec: {
        nodeSelector: {
          'kubernetes.io/os': 'linux'
        }
        containers: [
          {
            name: 'store-front'
            image: 'ghcr.io/azure-samples/aks-store-demo/store-front:latest'
            ports: [
              {
                containerPort: any(8080)
                name: 'store-front'
              }
            ]
            env: [
              {
                name: 'VUE_APP_ORDER_SERVICE_URL'
                value: 'http://order-service:3000/'
              }
              {
                name: 'VUE_APP_PRODUCT_SERVICE_URL'
                value: 'http://product-service:3002/'
              }
            ]
            resources: {
              requests: {
                cpu: '1m'
                memory: '200Mi'
              }
              limits: {
                cpu: '1000m'
                memory: '512Mi'
              }
            }
            startupProbe: {
              httpGet: {
                path: '/health'
                port: any(8080)
              }
              failureThreshold: 3
              initialDelaySeconds: 5
              periodSeconds: 5
            }
            readinessProbe: {
              httpGet: {
                path: '/health'
                port: any(8080)
              }
              failureThreshold: 3
              initialDelaySeconds: 3
              periodSeconds: 3
            }
            livenessProbe: {
              httpGet: {
                path: '/health'
                port: any(8080)
              }
              failureThreshold: 5
              initialDelaySeconds: 3
              periodSeconds: 3
            }
          }
        ]
      }
    }
  }
}

resource coreService_storeFront 'core/Service@v1' = {
  metadata: {
    name: 'store-front'
  }
  spec: {
    ports: [
      {
        port: any(80)
        targetPort: any(8080)
      }
    ]
    selector: {
      app: 'store-front'
    }
    type: 'LoadBalancer'
  }
}

resource appsDeployment_storeAdmin 'apps/Deployment@v1' = {
  metadata: {
    name: 'store-admin'
  }
  spec: {
    replicas: 1
    selector: {
      matchLabels: {
        app: 'store-admin'
      }
    }
    template: {
      metadata: {
        labels: {
          app: 'store-admin'
        }
      }
      spec: {
        nodeSelector: {
          'kubernetes.io/os': 'linux'
        }
        containers: [
          {
            name: 'store-admin'
            image: 'ghcr.io/azure-samples/aks-store-demo/store-admin:latest'
            ports: [
              {
                containerPort: any(8081)
                name: 'store-admin'
              }
            ]
            env: [
              {
                name: 'VUE_APP_PRODUCT_SERVICE_URL'
                value: 'http://product-service:3002/'
              }
              {
                name: 'VUE_APP_MAKELINE_SERVICE_URL'
                value: 'http://makeline-service:3001/'
              }
            ]
            resources: {
              requests: {
                cpu: '1m'
                memory: '200Mi'
              }
              limits: {
                cpu: '1000m'
                memory: '512Mi'
              }
            }
            startupProbe: {
              httpGet: {
                path: '/health'
                port: any(8081)
              }
              failureThreshold: 3
              initialDelaySeconds: 5
              periodSeconds: 5
            }
            readinessProbe: {
              httpGet: {
                path: '/health'
                port: any(8081)
              }
              failureThreshold: 3
              initialDelaySeconds: 3
              periodSeconds: 5
            }
            livenessProbe: {
              httpGet: {
                path: '/health'
                port: any(8081)
              }
              failureThreshold: 5
              initialDelaySeconds: 3
              periodSeconds: 3
            }
          }
        ]
      }
    }
  }
}

resource coreService_storeAdmin 'core/Service@v1' = {
  metadata: {
    name: 'store-admin'
  }
  spec: {
    ports: [
      {
        port: any(80)
        targetPort: any(8081)
      }
    ]
    selector: {
      app: 'store-admin'
    }
    type: 'LoadBalancer'
  }
}

resource appsDeployment_virtualCustomer 'apps/Deployment@v1' = {
  metadata: {
    name: 'virtual-customer'
  }
  spec: {
    replicas: 1
    selector: {
      matchLabels: {
        app: 'virtual-customer'
      }
    }
    template: {
      metadata: {
        labels: {
          app: 'virtual-customer'
        }
      }
      spec: {
        nodeSelector: {
          'kubernetes.io/os': 'linux'
        }
        containers: [
          {
            name: 'virtual-customer'
            image: 'ghcr.io/azure-samples/aks-store-demo/virtual-customer:latest'
            env: [
              {
                name: 'ORDER_SERVICE_URL'
                value: 'http://order-service:3000/'
              }
              {
                name: 'ORDERS_PER_HOUR'
                value: '100'
              }
            ]
            resources: {
              requests: {
                cpu: '1m'
                memory: '1Mi'
              }
              limits: {
                cpu: '2m'
                memory: '20Mi'
              }
            }
          }
        ]
      }
    }
  }
}

resource appsDeployment_virtualWorker 'apps/Deployment@v1' = {
  metadata: {
    name: 'virtual-worker'
  }
  spec: {
    replicas: 1
    selector: {
      matchLabels: {
        app: 'virtual-worker'
      }
    }
    template: {
      metadata: {
        labels: {
          app: 'virtual-worker'
        }
      }
      spec: {
        nodeSelector: {
          'kubernetes.io/os': 'linux'
        }
        containers: [
          {
            name: 'virtual-worker'
            image: 'ghcr.io/azure-samples/aks-store-demo/virtual-worker:latest'
            env: [
              {
                name: 'MAKELINE_SERVICE_URL'
                value: 'http://makeline-service:3001'
              }
              {
                name: 'ORDERS_PER_HOUR'
                value: '100'
              }
            ]
            resources: {
              requests: {
                cpu: '1m'
                memory: '1Mi'
              }
              limits: {
                cpu: '2m'
                memory: '20Mi'
              }
            }
          }
        ]
      }
    }
  }
}
