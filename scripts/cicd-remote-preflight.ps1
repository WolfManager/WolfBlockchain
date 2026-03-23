param(
    [string]$ExpectedStagingNamespace = "wolf-blockchain",
    [string]$ExpectedProductionNamespace = "wolf-blockchain"
)

$ErrorActionPreference = "Stop"

function Write-Ok($msg) { Write-Host "✅ $msg" -ForegroundColor Green }
function Write-Warn($msg) { Write-Host "⚠️  $msg" -ForegroundColor Yellow }
function Write-Err($msg) { Write-Host "❌ $msg" -ForegroundColor Red }

$failed = $false

Write-Host "== WolfBlockchain CI/CD Remote Preflight ==" -ForegroundColor Cyan

# 1) Core files
$requiredFiles = @(
    ".github/workflows/deploy.yml",
    "scripts/smoke-tests.sh",
    "scripts/staging-validate.ps1",
    "src/WolfBlockchain.API/Program.cs",
    "k8s/07-deployment.yaml",
    "k8s/09-ingress.yaml"
)

foreach ($f in $requiredFiles) {
    if (Test-Path $f) { Write-Ok "Found $f" }
    else { Write-Err "Missing $f"; $failed = $true }
}

# 2) Local toolchain
$tools = @("dotnet", "kubectl", "git")
foreach ($t in $tools) {
    if (Get-Command $t -ErrorAction SilentlyContinue) { Write-Ok "Tool available: $t" }
    else { Write-Err "Tool missing: $t"; $failed = $true }
}

# 3) Build check
try {
    dotnet build -c Release | Out-Null
    if ($LASTEXITCODE -eq 0) { Write-Ok "dotnet build -c Release passed" }
    else { Write-Err "dotnet build failed"; $failed = $true }
}
catch {
    Write-Err "dotnet build threw exception: $($_.Exception.Message)"
    $failed = $true
}

# 4) Workflow sanity checks
$workflowPath = ".github/workflows/deploy.yml"
if (Test-Path $workflowPath) {
    $wf = Get-Content $workflowPath -Raw

    $checks = @(
        "STAGING_NAMESPACE",
        "PRODUCTION_NAMESPACE",
        "KUBE_CONFIG_STAGING",
        "KUBE_CONFIG_PROD",
        "DOCKER_USERNAME",
        "DOCKER_PASSWORD",
        "deploy-staging",
        "deploy-production"
    )

    foreach ($c in $checks) {
        if ($wf.Contains($c)) { Write-Ok "Workflow contains: $c" }
        else { Write-Err "Workflow missing: $c"; $failed = $true }
    }
}

# 5) Kubernetes local readiness (best-effort)
try {
    $ns = kubectl get ns $ExpectedStagingNamespace --no-headers 2>$null
    if ($LASTEXITCODE -eq 0 -and $ns) { Write-Ok "Namespace exists: $ExpectedStagingNamespace" }
    else { Write-Warn "Namespace not found locally: $ExpectedStagingNamespace (remote cluster may still be fine)" }
}
catch {
    Write-Warn "Could not verify Kubernetes namespace locally"
}

# 6) Git repository status (best-effort)
try {
    git rev-parse --is-inside-work-tree 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Ok "Git repository initialized"

        git remote get-url origin 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) { Write-Ok "Git remote 'origin' configured" }
        else { Write-Warn "Git remote 'origin' not configured yet" }
    }
    else {
        Write-Warn "Not a git repository yet. Run: git init"
    }
}
catch {
    Write-Warn "Git checks could not be completed"
}

Write-Host "`n== Manual GitHub checks (required before remote deploy) ==" -ForegroundColor Cyan
Write-Host "Repository Variables:" 
Write-Host "  - STAGING_NAMESPACE = $ExpectedStagingNamespace"
Write-Host "  - PRODUCTION_NAMESPACE = $ExpectedProductionNamespace"
Write-Host "Secrets:"
Write-Host "  - DOCKER_USERNAME"
Write-Host "  - DOCKER_PASSWORD"
Write-Host "  - KUBE_CONFIG_STAGING"
Write-Host "  - KUBE_CONFIG_PROD"
Write-Host "  - SLACK_WEBHOOK (optional)"

if ($failed) {
    Write-Host "`nPreflight result: FAILED" -ForegroundColor Red
    exit 1
}

Write-Host "`nPreflight result: PASSED (local checks)" -ForegroundColor Green
exit 0