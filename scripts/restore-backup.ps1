# Restore WolfBlockchain from the latest backup for a given date (PowerShell)
# Usage: .\restore-backup.ps1 -BackupDate 20260404
# Usage: .\restore-backup.ps1 -BackupDate 20260404 -DryRun
# Usage: .\restore-backup.ps1 -BackupDate 20260404 -AppLabel wolf-blockchain-node -DryRun

param(
    [Parameter(Mandatory = $false)]
    [string]$BackupDate = "20260404",

    [Parameter(Mandatory = $false)]
    [string]$BackupRoot = "backups",

    [Parameter(Mandatory = $false)]
    [string]$Namespace = "wolf-blockchain",

    [Parameter(Mandatory = $false)]
    [string]$AppLabel = "wolf-blockchain-api",

    [Parameter(Mandatory = $false)]
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Write-Info($m) { Write-Host "[INFO] $m" -ForegroundColor Cyan }
function Write-Ok($m)   { Write-Host "[OK]   $m" -ForegroundColor Green }
function Write-Warn($m) { Write-Host "[WARN] $m" -ForegroundColor Yellow }
function Write-Err($m)  { Write-Host "[ERR]  $m" -ForegroundColor Red }

Write-Info "== WolfBlockchain Backup Restore =="
Write-Info "Target date : $BackupDate"
Write-Info "Backup root : $BackupRoot"
Write-Info "Namespace   : $Namespace"
if ($DryRun) { Write-Warn "DRY-RUN mode — no changes will be applied" }

# ── 1. Locate backup directories that match the requested date ──────────────
if (-not (Test-Path $BackupRoot)) {
    Write-Err "Backup root directory not found: $BackupRoot"
    exit 1
}

$matchingDirs = Get-ChildItem -Path $BackupRoot -Directory |
    Where-Object { $_.Name -match "-$BackupDate-" } |
    Sort-Object Name -Descending

if ($matchingDirs.Count -eq 0) {
    Write-Err "No backup directories found for date $BackupDate under '$BackupRoot'."
    Write-Info "Available backups:"
    Get-ChildItem -Path $BackupRoot -Directory | ForEach-Object { Write-Info "  $($_.Name)" }
    exit 1
}

$latest = $matchingDirs[0]
Write-Ok "Found $($matchingDirs.Count) backup(s) for $BackupDate — using latest: $($latest.Name)"

# ── 2. Verify expected manifest files are present ───────────────────────────
$manifests = @(
    "configmaps.yaml",
    "all.yaml",
    "hpa.yaml",
    "ingress.yaml"
)

$missing = @()
foreach ($file in $manifests) {
    $path = Join-Path $latest.FullName $file
    if (Test-Path $path) {
        Write-Ok "Manifest present: $file"
    } else {
        Write-Warn "Manifest missing (will skip): $file"
        $missing += $file
    }
}

$applyList = $manifests | Where-Object { $_ -notin $missing }

if ($applyList.Count -eq 0) {
    Write-Err "No manifests available to apply. Aborting."
    exit 1
}

# ── 3. Verify kubectl is available ──────────────────────────────────────────
if (-not (Get-Command kubectl -ErrorAction SilentlyContinue)) {
    Write-Err "kubectl not found. Please install kubectl and configure a valid kubeconfig."
    exit 1
}
Write-Ok "kubectl is available"

# ── 4. Apply manifests ───────────────────────────────────────────────────────
Write-Info "Applying $($applyList.Count) manifest(s) to namespace '$Namespace'..."

foreach ($file in $applyList) {
    $path = Join-Path $latest.FullName $file
    Write-Info "  kubectl apply -f $path -n $Namespace"

    if (-not $DryRun) {
        kubectl apply -f $path -n $Namespace
        if ($LASTEXITCODE -ne 0) {
            Write-Err "kubectl apply failed for $file (exit code $LASTEXITCODE)"
            exit 1
        }
        Write-Ok "Applied: $file"
    }
}

# ── 5. Wait for pods to become ready ────────────────────────────────────────
if (-not $DryRun) {
    Write-Info "Waiting for pods to be ready (timeout: 300s)..."
    kubectl wait --for=condition=Ready pod `
        -l app=$AppLabel `
        -n $Namespace `
        --timeout=300s

    if ($LASTEXITCODE -ne 0) {
        Write-Warn "Pods did not reach Ready state within the timeout."
        Write-Warn "Check status with: kubectl get pods -n $Namespace"
    } else {
        Write-Ok "All targeted pods are Ready"
    }

    Write-Info "Current pod status:"
    kubectl get pods -n $Namespace
}

# ── 6. Summary ───────────────────────────────────────────────────────────────
Write-Info ""
Write-Info "== Restore Summary =="
Write-Info "Backup used    : $($latest.FullName)"
Write-Info "Manifests applied : $($applyList -join ', ')"
if ($DryRun) {
    Write-Warn "DRY-RUN complete — no changes were applied."
} else {
    Write-Ok "Restore from backup '$($latest.Name)' completed successfully."
}
exit 0
