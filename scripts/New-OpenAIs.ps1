# Provisions Azure OpenAI Service instances to all available regions and deploys the model on those respective locations
Param(
    [string]
    [Parameter(Mandatory=$false)]
    $ResourceGroupLocation = "koreacentral",

    [string]
    [Parameter(Mandatory=$false)]
    $AzureEnvironmentName,

    [string]
    [Parameter(Mandatory=$false)]
    $ModelName = "gpt-4o",

    [string]
    [Parameter(Mandatory=$false)]
    $ModelVersion = "2024-11-20",

    [string]
    [Parameter(Mandatory=$false)]
    $ApiVersion = "2024-10-01",

    [switch]
    [Parameter(Mandatory=$false)]
    $Help
)

function Show-Usage {
    Write-Host "    This provisions Azure OpenAI Service instances to all available regions and deploys the model on those respective locations

    Usage: $(Split-Path $MyInvocation.ScriptName -Leaf) ``
            [-ResourceGroupLocation <Resource group location>] ``
            [-AzureEnvironmentName  <Azure environment name>] ``
            [-ModelName             <Azure OpenAI model name>] ``
            [-ModelVersion          <Azure OpenAI model version>] ``
            [-ApiVersion            <API version>] ``

            [-Help]

    Options:
        -ResourceGroupLocation  Resource group name. Default value is `'koreacentral`'
        -AzureEnvironmentName   Azure environment name.
        -ModelName              Azure OpenAI model name. Default value is `'gpt-4o`'
        -ModelVersion           Azure OpenAI model version. Default value is `'2024-11-20`'
        -ApiVersion             API version. Default value is `'2024-10-01`'

        -Help:                  Show this message.
"

    Exit 0
}

# Show usage
$needHelp = $Help -eq $true
if ($needHelp -eq $true) {
    Show-Usage
    Exit 0
}

if (($ResourceGroupLocation -eq $null) -or ($AzureEnvironmentName -eq $null) -or ($ModelName -eq $null) -or ($ModelVersion -eq $null) -or ($ApiVersion -eq $null)) {
    Show-Usage
    Exit 0
}

# Get resource token
$token = "abcdefghijklmnopqrstuvwxyz"
$subscriptionId = az account show --query "id" -o tsv
$baseValue = "$AzureEnvironmentName|$subscriptionId"
$hasher = [System.Security.Cryptography.HashAlgorithm]::Create('sha256')
$hash = $hasher.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($baseValue))
$hash | ForEach-Object { $calculated += $token[$_ % 26] }
$resourceToken = $($calculated).Substring(0, 13).ToLowerInvariant()

# Provision resource group
$resourceGroupName = "rg-$AzureEnvironmentName"

$resourceGroupExists = az group exists -n $resourceGroupName
if ($resourceGroupExists -eq $false) {
    $rg = az group create -n $resourceGroupName -l $ResourceGroupLocation
}

Write-Host "Provisioning $ModelName ..." -ForegroundColor Yellow

# Check available locations
$subscriptionId = az account show --query "id" -o tsv
$url = "/subscriptions/$subscriptionId/providers/Microsoft.CognitiveServices"
$skuName = "GlobalStandard"

$locations = az rest --method GET `
    --uri "$url/skus?api-version=$ApiVersion" `
    --query "value[?kind=='OpenAI'] | [?resourceType == 'accounts'].locations[0]" | ConvertFrom-Json

$locations | ForEach-Object {
    $location = $_.ToLowerInvariant()

    # Check available models
    $models = az rest --method GET `
        --uri "$url/locations/$location/models?api-version=$ApiVersion" `
        --query "sort_by(value[?kind == 'OpenAI'].{ name: model.name, version: model.version, skus: model.skus }, &name)" | ConvertFrom-Json

    $models | ForEach-Object {
        $model = $_
        $skus = $_.skus | Where-Object { $_.name -eq $skuName }
        if ($model.name -eq $ModelName -and $model.version -eq $ModelVersion -and $skus.Count -gt 0) {
            # Provision Azure OpenAI Services
            $cogsvc = az cognitiveservices account list -g $ResourceGroupName --query "[?location=='$location']" | ConvertFrom-Json
            if ($cogsvc -eq $null) {
                $resourceName = "cogsvc-$resourceToken-$location"

                Write-Host "Provisioning $resourceName instance ..." -ForegroundColor Cyan

                $cogsvc = az cognitiveservices account create `
                    -g $resourceGroupName `
                    -n $resourceName `
                    -l $location `
                    --kind OpenAI `
                    --sku S0 `
                    --assign-identity `
                    --tags azd-env-name=cogsvc-$AzureEnvironmentName | ConvertFrom-Json

                Write-Host "    $resourceName instance has been provisioned" -ForegroundColor Cyan
            }

            $deploymentName = $ModelName

            $deployment = az cognitiveservices account deployment list `
                -g $resourceGroupName `
                -n "cogsvc-$resourceToken-$location" `
                --query "[?name=='$deploymentName']" | ConvertFrom-Json

            # Check model capacity
            $sku = $skus | Where-Object { $_.name -eq "GlobalStandard" }
            $capacity = $sku.capacity.default

            # Deploy model
            if ($deployment -eq $null) {
                Write-Host "Provisioning $deploymentName on the $($cogsvc.name) instance ..." -ForegroundColor Magenta

                $deployment = az cognitiveservices account deployment create `
                    -g $resourceGroupName `
                    -n "cogsvc-$resourceToken-$location" `
                    --model-format OpenAI `
                    --model-name $ModelName `
                    --model-version $ModelVersion `
                    --deployment-name $deploymentName `
                    --sku-name $skuName `
                    --sku-capacity $capacity

                Write-Host "    $deploymentName on the $($cogsvc.name) instance has been deployed" -ForegroundColor Magenta
            }
        }
    }
}

Write-Host "$ModelName has been provisioned" -ForegroundColor Yellow
