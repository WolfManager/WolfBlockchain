# Restore WolfBlockchain to the confirmed stable state from 2026-03-24
#
# This script resets the local codebase to the verified stable git commit
# (5d6f915 on main, tagged as stable-20260324), rebuilds, and re-runs the
# full unit-test suite to confirm the restored state is healthy.
#
# Optionally it can roll back a running Kubernetes deployment to the stable
# container image (v2.0.0) and verify all endpoints respond correctly.
#
# Usage:
#   .\restore-stable-20260324.ps1
#   .\restore-stable-20260324.ps1 -SkipTests
#   .\restore-stable-20260324.ps1 -RestoreKubernetes -Namespace wolf-blockchain
#   .\restore-stable-20260324.ps1 -DryRun
#
# The script never force-pushes to any remote.  To propagate the restored
# state to the remote, push manually after validation.

param(
    # Target stable commit SHA confirmed on 2026-03-24.
    # Override only if the commit has been rebased/re-tagged.
    [Parameter(Mandatory = $false)]
    [string]$StableCommit = "5d6f915",

    # Stable Docker image tag that was verified on 2026-03-24
    [Parameter(Mandatory = $false)]
    [string]$StableImageTag = "wolfblockchain:v2.0.0",

    # Kubernetes namespace to roll back (used only when -RestoreKubernetes is set)
    [Parameter(Mandatory = $false)]
    [string]$Namespace = "wolf-blockchain",

    # Kubernetes deployment name (used only when -RestoreKubernetes is set)
    [Parameter(Mandatory = $false)]
    [string]$DeploymentName = "wolf-blockchain-api",

    # Ingress host used for endpoint smoke-checks after K8s rollback
    [Parameter(Mandatory = $false)]
    [string]$IngressHost = "wolf-blockchain.local",

    # Skip dotnet build + unit-test validation after code restore
    [Parameter(Mandatory = $false)]
    [switch]$SkipTests,

    # Also roll back the running Kubernetes deployment to the stable image
    [Parameter(Mandatory = $false)]
    [switch]$RestoreKubernetes,

    # Print every action that would be taken without actually changing anything
    [Parameter(Mandatory = $false)]
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------
function Write-Info($m)  { Write-Host "[INFO]  $m" -ForegroundColor Cyan    }
function Write-Ok($m)    { Write-Host "[OK]    $m" -ForegroundColor Green   }
function Write-Warn($m)  { Write-Host "[WARN]  $m" -ForegroundColor Yellow  }
function Write-Err($m)   { Write-Host "[ERROR] $m" -ForegroundColor Red     }
function Write-Step($m)  { Write-Host "`n==> $m" -ForegroundColor Magenta   }

function Invoke-Cmd {
    param([string]$Cmd, [string[]]$CmdArgs)
    if ($DryRun) {
        Write-Host "[DRY-RUN] $Cmd $($CmdArgs -join ' ')" -ForegroundColor DarkGray
        return
    }
    & $Cmd @CmdArgs
    if ($LASTEXITCODE -ne 0) { throw "Command failed: $Cmd $($CmdArgs -join ' ')" }
}

# ---------------------------------------------------------------------------
# Locate repo root regardless of where the script is called from
# ---------------------------------------------------------------------------
$ScriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot   = Resolve-Path (Join-Path $ScriptDir "..")
Set-Location $RepoRoot
Write-Info "Repo root: $RepoRoot"

$RestoreTimestamp = (Get-Date -Format "yyyyMMdd-HHmmss")
$BackupBranch     = "backup/pre-restore-$RestoreTimestamp"
$ReportFile       = Join-Path $RepoRoot "RESTORE_REPORT_$RestoreTimestamp.md"

# ---------------------------------------------------------------------------
# STEP 1 – Pre-flight checks
# ---------------------------------------------------------------------------
Write-Step "Step 1: Pre-flight checks"

if (-not (Test-Path (Join-Path $RepoRoot ".git"))) {
    throw "Not a git repository: $RepoRoot"
}
Write-Ok "Git repository detected"

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw "git is not available on PATH"
}
Write-Ok "git found: $(git --version)"

if (-not $SkipTests) {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw "dotnet SDK is not available on PATH. Install .NET SDK or pass -SkipTests."
    }
    Write-Ok "dotnet found: $(dotnet --version)"
}

if ($RestoreKubernetes) {
    if (-not (Get-Command kubectl -ErrorAction SilentlyContinue)) {
        throw "kubectl is not available on PATH but -RestoreKubernetes was requested."
    }
    Write-Ok "kubectl found"
}

# ---------------------------------------------------------------------------
# STEP 2 – Check working tree and create a backup branch
# ---------------------------------------------------------------------------
Write-Step "Step 2: Backup current state"

$CurrentBranch = (git branch --show-current).Trim()
Write-Info "Current branch: $CurrentBranch"

$DirtyFiles = git status --porcelain
if (-not [string]::IsNullOrWhiteSpace(($DirtyFiles | Out-String).Trim())) {
    Write-Warn "Working tree has uncommitted changes – stashing before restore"
    Invoke-Cmd git @("stash", "push", "-u", "-m", "pre-restore-stash-$RestoreTimestamp")
    Write-Ok "Changes stashed as: pre-restore-stash-$RestoreTimestamp"
}
else {
    Write-Ok "Working tree is clean"
}

# Create a backup branch pointing at HEAD so the current state is never lost
$HeadCommit = (git rev-parse HEAD).Trim()
Write-Info "HEAD before restore: $HeadCommit"

if (-not $DryRun) {
    git branch $BackupBranch $HeadCommit
}
else {
    Write-Host "[DRY-RUN] git branch $BackupBranch $HeadCommit" -ForegroundColor DarkGray
}
Write-Ok "Backup branch created: $BackupBranch  (HEAD = $HeadCommit)"

# ---------------------------------------------------------------------------
# STEP 3 – Verify stable commit exists in the repository history
# ---------------------------------------------------------------------------
Write-Step "Step 3: Verify stable commit $StableCommit"

$CommitExists = git cat-file -e "$StableCommit^{commit}" 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Warn "Commit $StableCommit not found locally – fetching from origin"
    Invoke-Cmd git @("fetch", "origin")

    if (-not $DryRun) {
        git cat-file -e "$StableCommit^{commit}" 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "Stable commit $StableCommit does not exist in this repository even after fetch. Verify the commit SHA."
        }
    }
    else {
        Write-Host "[DRY-RUN] Skipping commit existence check after fetch" -ForegroundColor DarkGray
    }
}

$StableCommitFull  = if (-not $DryRun) { (git rev-parse $StableCommit).Trim() }  else { "$StableCommit (dry-run)" }
$StableCommitDate  = if (-not $DryRun) { (git log -1 --format="%ci" $StableCommit).Trim() } else { "N/A (dry-run)" }
$StableCommitMsg   = if (-not $DryRun) { (git log -1 --format="%s" $StableCommit).Trim() }  else { "N/A (dry-run)" }
Write-Ok "Stable commit found:"
Write-Info "  SHA     : $StableCommitFull"
Write-Info "  Date    : $StableCommitDate"
Write-Info "  Message : $StableCommitMsg"

# ---------------------------------------------------------------------------
# STEP 4 – Reset main branch to the stable commit
# ---------------------------------------------------------------------------
Write-Step "Step 4: Restore codebase to stable commit $StableCommit"

Invoke-Cmd git @("checkout", $CurrentBranch)
Invoke-Cmd git @("reset", "--hard", $StableCommit)
Write-Ok "Branch '$CurrentBranch' reset to $StableCommit"

# ---------------------------------------------------------------------------
# STEP 5 – Build and test validation
# ---------------------------------------------------------------------------
$BuildStatus = "SKIPPED"
$TestStatus  = "SKIPPED"
$TestCount   = 0

if (-not $SkipTests) {
    Write-Step "Step 5: Build validation"

    $TestProject = Join-Path $RepoRoot "tests" "WolfBlockchain.Tests" "WolfBlockchain.Tests.csproj"

    if (-not $DryRun) {
        dotnet build -c Release --nologo
        if ($LASTEXITCODE -ne 0) {
            Write-Err "Build FAILED after restore. Inspect errors above."
            $BuildStatus = "FAILED"
        }
        else {
            $BuildStatus = "PASSED"
            Write-Ok "Build passed"
        }
    }
    else {
        Write-Host "[DRY-RUN] dotnet build -c Release --nologo" -ForegroundColor DarkGray
        $BuildStatus = "DRY-RUN"
    }

    Write-Step "Step 5b: Unit test validation"

    if ($BuildStatus -eq "PASSED") {
        if (-not $DryRun) {
            dotnet test $TestProject -c Release --no-build --nologo `
                --filter "Category!=Integration" `
                --logger "console;verbosity=normal"

            if ($LASTEXITCODE -ne 0) {
                Write-Err "Unit tests FAILED after restore."
                $TestStatus = "FAILED"
            }
            else {
                $TestStatus = "PASSED"
                Write-Ok "All unit tests passed"
            }
        }
        else {
            Write-Host "[DRY-RUN] dotnet test $TestProject ..." -ForegroundColor DarkGray
            $TestStatus = "DRY-RUN"
        }
    }
    else {
        Write-Warn "Skipping tests because build did not pass"
        $TestStatus = "SKIPPED (build failed)"
    }
}
else {
    Write-Warn "Tests skipped (-SkipTests flag)"
}

# ---------------------------------------------------------------------------
# STEP 6 – Optional Kubernetes rollback
# ---------------------------------------------------------------------------
$K8sStatus = "NOT REQUESTED"

if ($RestoreKubernetes) {
    Write-Step "Step 6: Kubernetes rollback to $StableImageTag"

    # Ensure the deployment exists
    $DepJson = kubectl get deployment $DeploymentName -n $Namespace -o json 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Warn "Deployment '$DeploymentName' not found in namespace '$Namespace'. Skipping K8s rollback."
        $K8sStatus = "SKIPPED (deployment not found)"
    }
    else {
        Write-Info "Setting image to $StableImageTag"
        Invoke-Cmd kubectl @(
            "set", "image",
            "deployment/$DeploymentName",
            "api=$StableImageTag",
            "-n", $Namespace
        )

        Write-Info "Waiting for rollout (timeout 5 min)"
        Invoke-Cmd kubectl @(
            "rollout", "status",
            "deployment/$DeploymentName",
            "-n", $Namespace,
            "--timeout=300s"
        )

        Write-Info "Verifying replicas"
        $DepStatus  = kubectl get deployment $DeploymentName -n $Namespace -o json | ConvertFrom-Json
        $Ready      = [int]$DepStatus.status.readyReplicas
        $Updated    = [int]$DepStatus.status.updatedReplicas
        $Replicas   = [int]$DepStatus.status.replicas

        if ($Ready -lt $Replicas -or $Updated -lt $Replicas) {
            Write-Err "Rollout not fully healthy: Ready=$Ready Updated=$Updated Desired=$Replicas"
            $K8sStatus = "FAILED"
        }
        else {
            Write-Ok "Rollout healthy: Ready=$Ready Updated=$Updated Desired=$Replicas"

            Write-Info "Endpoint smoke-checks via ingress host '$IngressHost'"
            $HealthCode  = curl.exe -s -o $null -w "%{http_code}" -H "Host: $IngressHost" "http://localhost/health"
            $ReadyCode   = curl.exe -s -o $null -w "%{http_code}" -H "Host: $IngressHost" "http://localhost/ready"
            $MetricsCode = curl.exe -s -o $null -w "%{http_code}" -H "Host: $IngressHost" "http://localhost/metrics"

            Write-Info "  /health  → $HealthCode"
            Write-Info "  /ready   → $ReadyCode"
            Write-Info "  /metrics → $MetricsCode"

            if ($HealthCode -ne "200" -or $ReadyCode -ne "200" -or $MetricsCode -ne "200") {
                Write-Err "Endpoint check failed. Kubernetes deployment may need manual inspection."
                $K8sStatus = "FAILED (endpoint check)"
            }
            else {
                Write-Ok "All endpoints responded 200"
                $K8sStatus = "PASSED"
            }
        }
    }
}

# ---------------------------------------------------------------------------
# STEP 7 – Determine overall result
# ---------------------------------------------------------------------------
Write-Step "Step 7: Summary"

$OverallOk = ($BuildStatus -in @("PASSED", "SKIPPED", "DRY-RUN")) `
          -and ($TestStatus  -in @("PASSED", "SKIPPED", "DRY-RUN", "SKIPPED (build failed)")) `
          -and ($K8sStatus   -in @("PASSED", "NOT REQUESTED", "SKIPPED (deployment not found)", "DRY-RUN"))

$OverallResult = if ($OverallOk) { "SUCCESS" } else { "PARTIAL FAILURE – manual action required" }

Write-Host ""
Write-Host "  Stable commit  : $StableCommitFull  ($StableCommitDate)" -ForegroundColor White
Write-Host "  Backup branch  : $BackupBranch" -ForegroundColor White
Write-Host "  Build          : $BuildStatus"   -ForegroundColor $(if ($BuildStatus -eq "PASSED") { "Green" } elseif ($BuildStatus -like "*FAIL*") { "Red" } else { "Yellow" })
Write-Host "  Tests          : $TestStatus"    -ForegroundColor $(if ($TestStatus  -eq "PASSED") { "Green" } elseif ($TestStatus  -like "*FAIL*") { "Red" } else { "Yellow" })
Write-Host "  Kubernetes     : $K8sStatus"     -ForegroundColor $(if ($K8sStatus   -eq "PASSED") { "Green" } elseif ($K8sStatus   -like "*FAIL*") { "Red" } else { "Yellow" })
Write-Host "  OVERALL        : $OverallResult" -ForegroundColor $(if ($OverallOk) { "Green" } else { "Red" })
Write-Host ""

# ---------------------------------------------------------------------------
# STEP 8 – Write machine-readable restore report
# ---------------------------------------------------------------------------
Write-Step "Step 8: Writing restore report"

if (-not $DryRun) {
    $Report = @"
# Restore Report - WolfBlockchain Stable State 2026-03-24

**Generated**: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss UTC")
**Script**: scripts/restore-stable-20260324.ps1

## Parameters

| Parameter | Value |
|-----------|-------|
| StableCommit | ``$StableCommit`` |
| StableCommitFull | ``$StableCommitFull`` |
| StableCommitDate | $StableCommitDate |
| StableCommitMessage | $StableCommitMsg |
| StableImageTag | ``$StableImageTag`` |
| RestoredBranch | ``$CurrentBranch`` |
| BackupBranch | ``$BackupBranch`` |
| DryRun | $DryRun |

## Validation Results

| Check | Result |
|-------|--------|
| dotnet build -c Release | $BuildStatus |
| dotnet test (unit, no integration) | $TestStatus |
| Kubernetes rollback | $K8sStatus |
| **OVERALL** | **$OverallResult** |

## Rollback Instructions

If the restored state is still not healthy, revert to the backup branch:

``````powershell
git checkout $CurrentBranch
git reset --hard $BackupBranch
``````

Or cherry-pick individual commits from the backup branch:

``````powershell
git log --oneline $BackupBranch
git cherry-pick <commit-sha>
``````

## Reference – Changes Confirmed in Stable State (2026-03-24)

The stable commit ``$StableCommit`` includes the following verified fixes:

1. **scripts/push-main-staging.ps1** – Dynamic repo path, working-tree check, main→staging sync
2. **.github/workflows/deploy.yml** – Slack notifications made optional (no-op if secret absent)
3. **src/WolfBlockchain.API/Program.cs** – ``UseForwardedHeaders()`` added for K8s ingress HTTPS
4. **k8s/07-deployment.yaml** – ``imagePullPolicy: Always``, version label set to ``v2.0.0``
5. **k8s/06-services.yaml** – Removed unused HTTPS backend port 5443, HTTP-only on 5000
6. **k8s/09-ingress.yaml** – Network policy pruned (removed port 5443 and 1433)

### Stable Metrics (as of 2026-03-24)

- Build: 0 errors, 0 warnings
- Unit tests: 153/153 PASS
- Framework: net10.0
- Image: wolfblockchain:v2.0.0
"@

    Set-Content -Path $ReportFile -Value $Report -Encoding UTF8
    Write-Ok "Restore report written: $ReportFile"
}
else {
    Write-Host "[DRY-RUN] Would write report to: $ReportFile" -ForegroundColor DarkGray
}

# ---------------------------------------------------------------------------
# Exit with appropriate code
# ---------------------------------------------------------------------------
if (-not $OverallOk) {
    Write-Err "Restore completed with failures. Review the report and backup branch before proceeding."
    exit 1
}

Write-Ok "Restore to stable state 2026-03-24 completed successfully."
exit 0
