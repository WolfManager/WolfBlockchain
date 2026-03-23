# Deploy WolfBlockchain to Kubernetes (PowerShell)
# Usage: .\deploy.ps1 -Environment staging -Version v2.0.0 -DockerImage wolfblockchain:staging

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("development", "staging", "production")]
    [string]$Environment,
    
    [Parameter(Mandatory=$false)]
    [string]$Version = "v2.0.0",
    
    [Parameter(Mandatory=$false)]
    [string]$DockerImage = "wolfblockchain:latest",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipTests,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipHealthCheck
)

# Colors for output
$ErrorColor = "Red"
$SuccessColor = "Green"
$WarningColor = "Yellow"
$InfoColor = "Cyan"

function Write-Header {
    param([string]$Message)
    Write-Host "`n" -NoNewline
    Write-Host "════════════════════════════════════════" -ForegroundColor $InfoColor
    Write-Host $Message -ForegroundColor $InfoColor
    Write-Host "════════════════════════════════════════" -ForegroundColor $InfoColor
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor $SuccessColor
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor $ErrorColor
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor $WarningColor
}

# Main deployment logic
Write-Header "🚀 WolfBlockchain Deployment"

# Step 1: Validate environment
Write-Host "Step 1: Validating deployment environment..." -ForegroundColor $InfoColor

$namespace = "wolf-blockchain-$Environment"
$kubeContext = "docker-desktop"

# Check kubectl
if (-not (Get-Command kubectl -ErrorAction SilentlyContinue)) {
    Write-Error "kubectl not found. Please install kubectl."
    exit 1
}

Write-Success "kubectl is available"

# Check namespace exists or create
$nsCheck = kubectl get namespace $namespace -ErrorAction SilentlyContinue
if (-not $nsCheck) {
    Write-Host "Namespace $namespace not found. Creating..." -ForegroundColor $WarningColor
    kubectl create namespace $namespace
    Write-Success "Namespace created"
} else {
    Write-Success "Namespace $namespace exists"
}

# Step 2: Run tests (unless skipped)
if (-not $SkipTests) {
    Write-Header "Step 2: Running Tests"
    
    Write-Host "Running unit tests..." -ForegroundColor $InfoColor
    dotnet test tests\WolfBlockchain.Tests\WolfBlockchain.Tests.csproj -c Release --filter "Category!=Integration"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Tests failed. Aborting deployment."
        exit 1
    }
    
    Write-Success "All tests passed"
} else {
    Write-Warning "Tests skipped (use -SkipTests flag)"
}

# Step 3: Update K8s deployment
Write-Header "Step 3: Updating Kubernetes Deployment"

Write-Host "Setting image to: $DockerImage" -ForegroundColor $InfoColor
kubectl set image deployment/wolf-blockchain-api `
    api=$DockerImage `
    -n $namespace `
    --record

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to update deployment"
    exit 1
}

Write-Success "Deployment image updated"

# Step 4: Wait for rollout
Write-Header "Step 4: Waiting for Rollout"

Write-Host "Waiting for pods to be ready (timeout: 5 minutes)..." -ForegroundColor $InfoColor
kubectl rollout status deployment/wolf-blockchain-api `
    -n $namespace `
    --timeout=300s

if ($LASTEXITCODE -ne 0) {
    Write-Error "Rollout failed. Check kubectl logs:"
    Write-Host "kubectl logs -n $namespace deployment/wolf-blockchain-api" -ForegroundColor $WarningColor
    exit 1
}

Write-Success "Rollout completed successfully"

# Step 5: Get pod status
Write-Header "Step 5: Verifying Pod Status"

$pods = kubectl get pods -n $namespace -o json | ConvertFrom-Json
$readyCount = ($pods.items | Where-Object { $_.status.phase -eq "Running" } | Measure-Object).Count
$totalCount = ($pods.items | Measure-Object).Count

Write-Host "Pods Ready: $readyCount / $totalCount" -ForegroundColor $InfoColor

if ($readyCount -eq $totalCount) {
    Write-Success "All pods are ready"
} else {
    Write-Warning "Not all pods are ready. Check status with: kubectl get pods -n $namespace"
}

# Step 6: Health check (unless skipped)
if (-not $SkipHealthCheck) {
    Write-Header "Step 6: Running Health Checks"
    
    $apiUrl = switch ($Environment) {
        "development" { "http://localhost" }
        "staging" { "https://staging.wolf-blockchain.local" }
        "production" { "https://api.wolf-blockchain.com" }
    }
    
    Write-Host "Checking API health at: $apiUrl" -ForegroundColor $InfoColor
    
    $maxRetries = 5
    $retryCount = 0
    
    while ($retryCount -lt $maxRetries) {
        try {
            $response = Invoke-WebRequest -Uri "$apiUrl/health" -UseBasicParsing -TimeoutSec 10
            if ($response.StatusCode -eq 200) {
                Write-Success "Health check passed"
                break
            }
        } catch {
            $retryCount++
            if ($retryCount -lt $maxRetries) {
                Write-Warning "Health check attempt $retryCount failed. Retrying in 10 seconds..."
                Start-Sleep -Seconds 10
            }
        }
    }
    
    if ($retryCount -eq $maxRetries) {
        Write-Error "Health check failed after $maxRetries attempts"
        exit 1
    }
} else {
    Write-Warning "Health checks skipped"
}

# Step 7: Summary
Write-Header "✅ Deployment Complete"

Write-Host "Environment: $Environment" -ForegroundColor $InfoColor
Write-Host "Namespace: $namespace" -ForegroundColor $InfoColor
Write-Host "Version: $Version" -ForegroundColor $InfoColor
Write-Host "Docker Image: $DockerImage" -ForegroundColor $InfoColor

Write-Host "`nUseful commands:" -ForegroundColor $InfoColor
Write-Host "  View pods: kubectl get pods -n $namespace"
Write-Host "  View logs: kubectl logs -n $namespace deployment/wolf-blockchain-api"
Write-Host "  Watch status: kubectl get pods -n $namespace -w"
Write-Host "  Describe pods: kubectl describe pods -n $namespace"

Write-Success "Deployment finished successfully!"
exit 0
