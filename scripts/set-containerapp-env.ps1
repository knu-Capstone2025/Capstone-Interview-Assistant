param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupPrefix,
    
    [Parameter(Mandatory=$true)]
    [string]$ApplicationInsightsConnectionString,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipLoginCheck
)

if (-not $SkipLoginCheck) {
    Write-Host "🔍 Checking Azure CLI login status..."
    $loginStatus = az account show 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Not logged in to Azure CLI. Please login before running this script."
        Write-Host "Login error: $loginStatus"
        exit 1
    }
    Write-Host "✅ Azure login confirmed"
}

# Set resource group name
$resourceGroup = "rg-$ResourceGroupPrefix"

Write-Host "🔍 Searching for Container Apps in resource group $resourceGroup..."

# Check if resource group exists
try {
    $rgExists = (az group exists --name "$resourceGroup" 2>/dev/null) -eq "true"
    if (-not $rgExists) {
        # Try alternative name patterns
        Write-Host "🔍 Searching for alternative resource groups..."
        $possibleGroups = az group list --query "[?contains(name,'$ResourceGroupPrefix') || contains(name,'interview') || contains(name,'capstone')].name" -o tsv
        if ($possibleGroups) {
            $resourceGroup = $possibleGroups -split "\n" | Select-Object -First 1
            Write-Host "✅ Found resource group: $resourceGroup"
        } else {
            Write-Host "❌ Resource group not found."
            exit 1
        }
    }
} catch {
    Write-Host "❌ Error checking resource group: $_"
    exit 1
}

# Get Container Apps list
try {
    Write-Host "🔍 Retrieving Container Apps..."
    $containerApps = az containerapp list --resource-group $resourceGroup --query "[].name" -o tsv
    if ([string]::IsNullOrEmpty($containerApps)) {
        Write-Host "❌ No Container Apps found"
        exit 1
    }
    Write-Host "✅ Found Container Apps"
} catch {
    Write-Host "❌ Error retrieving Container Apps: $_"
    exit 1
}

# Set environment variables for each Container App
foreach ($app in $containerApps -split "\n") {
    if (-not [string]::IsNullOrWhiteSpace($app)) {
        Write-Host "🔧 Setting environment variables for '$app'..."
        
        az containerapp update --name "$app" --resource-group "$resourceGroup" `
          --set-env-vars "APPLICATIONINSIGHTS_CONNECTION_STRING=$ApplicationInsightsConnectionString" `
                        --only-show-errors --output none
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Successfully configured '$app'"
        } else {
            Write-Host "❌ Failed to configure '$app'"
        }
    }
}

Write-Host "✅ Container Apps environment variables configuration completed"
