param(
    [Parameter(Mandatory = $false)]
    [string]$Namespace = "wolf-blockchain",

    [Parameter(Mandatory = $false)]
    [string]$DeploymentName = "wolf-blockchain-api",

    [Parameter(Mandatory = $false)]
    [string]$ImageTag = "wolfblockchain:v2.0.1-rc3",

    [Parameter(Mandatory = $false)]
    [string]$IngressHost = "wolf-blockchain.local"
)

$ErrorActionPreference = "Stop"

function Write-Info($m) { Write-Host "[INFO] $m" -ForegroundColor Cyan }
function Write-Ok($m) { Write-Host "[OK] $m" -ForegroundColor Green }
function Write-Warn($m) { Write-Host "[WARN] $m" -ForegroundColor Yellow }

Write-Info "Starting production promotion checks"

if (Get-Command docker -ErrorAction SilentlyContinue)
{
    docker image inspect $ImageTag *> $null
    if ($LASTEXITCODE -ne 0)
    {
        Write-Warn "Image '$ImageTag' not found locally. In local clusters with IfNotPresent this can cause ImagePullBackOff unless the image exists in registry."
    }
    else
    {
        Write-Ok "Image '$ImageTag' exists locally"
    }
}

$requiredSecretKeys = @(
    "ConnectionStrings__DefaultConnection",
    "JWT_SECRET",
    "Security__BootstrapToken",
    "RPC_PRIMARY",
    "RPC_FALLBACK"
)

$secret = kubectl get secret wolf-blockchain-secrets -n $Namespace -o json | ConvertFrom-Json
$decoded = @{}
foreach ($prop in $secret.data.PSObject.Properties)
{
    $decoded[$prop.Name] = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($prop.Value))
}

$missing = @()
$placeholder = @()
foreach ($key in $requiredSecretKeys)
{
    if (-not $decoded.ContainsKey($key) -or [string]::IsNullOrWhiteSpace($decoded[$key]))
    {
        $missing += $key
        continue
    }

    if ($decoded[$key] -like "__SET_IN_CLUSTER_ONLY__" -or $decoded[$key] -like "__*__")
    {
        $placeholder += $key
    }
}

if ($missing.Count -gt 0)
{
    throw "Missing required secret keys: $($missing -join ', ')"
}

if ($placeholder.Count -gt 0)
{
    throw "Placeholder values detected for keys: $($placeholder -join ', '). Inject real production values before promotion."
}

Write-Ok "Required secret keys are present and non-placeholder"

Write-Info "Updating deployment image to $ImageTag"
kubectl set image deployment/$DeploymentName api=$ImageTag -n $Namespace

Write-Info "Waiting for rollout completion"
kubectl rollout status deployment/$DeploymentName -n $Namespace --timeout=300s

Write-Info "Verifying ready/updated replicas"
$status = kubectl get deployment $DeploymentName -n $Namespace -o json | ConvertFrom-Json
$ready = [int]($status.status.readyReplicas)
$updated = [int]($status.status.updatedReplicas)
$replicas = [int]($status.status.replicas)

if ($ready -lt $replicas -or $updated -lt $replicas)
{
    throw "Rollout not fully healthy. Ready=$ready Updated=$updated Replicas=$replicas"
}

Write-Ok "Rollout healthy: Ready=$ready Updated=$updated Replicas=$replicas"

Write-Info "Running endpoint checks via ingress"
$health = curl.exe -s -o NUL -w "%{http_code}" -H "Host: $IngressHost" "http://localhost/health"
$readyCode = curl.exe -s -o NUL -w "%{http_code}" -H "Host: $IngressHost" "http://localhost/ready"
$metrics = curl.exe -s -o NUL -w "%{http_code}" -H "Host: $IngressHost" "http://localhost/metrics"

if ($health -ne "200" -or $readyCode -ne "200" -or $metrics -ne "200")
{
    throw "Endpoint check failed: health=$health ready=$readyCode metrics=$metrics"
}

Write-Ok "Endpoint checks passed: health=$health ready=$readyCode metrics=$metrics"
Write-Ok "Production promotion sequence completed successfully"