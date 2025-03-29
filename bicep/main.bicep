@description('Azure OpenAI 서비스 이름')
param openAiServiceName string = 'openai-${uniqueString(resourceGroup().id)}'

@description('Azure OpenAI 서비스 위치. Default: swedencentral')
param openAiLocation string = 'swedencentral'

@description('GPT-4o 모델 배포 이름. Default: gpt4o')
param gpt4oDeploymentName string = 'gpt4o'

@description('Azure OpenAI 서비스 SKU 이름. Default: S0')
param openAiSkuName string = 'S0'

@description('Azure OpenAI 서비스 SKU 용량. Default: 5')
param openAiSkuCapacity int = 5

@description('GPT-4o 모델 배포 SKU 이름. Default: Standard')
param gpt4oSkuName string = 'Standard'

@description('GPT-4o 모델 배포 SKU 용량. Default: 5')
param gpt4oSkuCapacity int = 5

// Azure OpenAI 서비스
resource openAiService 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: openAiServiceName
  location: openAiLocation
  kind: 'OpenAI'
  sku: {
    name: openAiSkuName
    capacity: openAiSkuCapacity
  }
  properties: {
    customSubDomainName: openAiServiceName
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
}

// GPT-4o 모델 배포
resource gpt4oDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: openAiService
  name: gpt4oDeploymentName
  sku: {
    name: gpt4oSkuName
    capacity: gpt4oSkuCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-11-20'
    }
    raiPolicyName: 'Microsoft.Default'
  }
}

// 출력 값
output openAiServiceName string = openAiService.name
output openAiEndpoint string = openAiService.properties.endpoint
output openAiDeploymentName string = gpt4oDeployment.name
