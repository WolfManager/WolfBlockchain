param(
    [Parameter(Mandatory = $false)]
    [string]$RemoteUrl,
    [string]$MainBranch = "main",
    [string]$StagingBranch = "staging",
    [string]$TriggerCommitMessage = "chore: trigger staging deploy pipeline"
)

$ErrorActionPreference = "Stop"

function Write-Info($m) { Write-Host "ℹ️  $m" -ForegroundColor Cyan }
function Write-Ok($m) { Write-Host "✅ $m" -ForegroundColor Green }
function Write-Warn($m) { Write-Host "⚠️  $m" -ForegroundColor Yellow }

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot "..")
Set-Location $repoRoot

# Ensure repo exists
if (-not (Test-Path ".git")) {
    throw "Current workspace is not a git repository. Run: git init"
}

$originalBranch = (git branch --show-current).Trim()
$workingTreeStatus = git status --porcelain
if ($LASTEXITCODE -ne 0) {
    throw "Unable to determine git status."
}

if (-not [string]::IsNullOrWhiteSpace(($workingTreeStatus | Out-String).Trim())) {
    throw "Working tree has uncommitted changes. Commit or stash them before running this script."
}

try {
    # Configure remote if URL provided
    $existingRemote = git remote get-url origin 2>$null
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($existingRemote)) {
        if ([string]::IsNullOrWhiteSpace($RemoteUrl)) {
            throw "Remote 'origin' is not configured. Provide -RemoteUrl <repo-url>."
        }

        git remote add origin $RemoteUrl
        Write-Ok "Added remote origin: $RemoteUrl"
    }
    else {
        Write-Info "Remote origin already configured: $existingRemote"
    }

    Write-Info "Checking out $MainBranch..."
    git checkout $MainBranch | Out-Null

    Write-Info "Pushing $MainBranch..."
    git push -u origin $MainBranch
    Write-Ok "Pushed $MainBranch"

    Write-Info "Syncing $StagingBranch from $MainBranch..."
    git checkout -B $StagingBranch $MainBranch | Out-Null

    Write-Info "Creating empty trigger commit on $StagingBranch..."
    git commit --allow-empty -m $TriggerCommitMessage | Out-Null

    Write-Info "Pushing $StagingBranch..."
    git push -u origin $StagingBranch --force-with-lease
    Write-Ok "Pushed $StagingBranch with trigger commit"
}
finally {
    if (-not [string]::IsNullOrWhiteSpace($originalBranch) -and $originalBranch -ne (git branch --show-current).Trim()) {
        git checkout $originalBranch | Out-Null
        Write-Ok "Returned to original branch: $originalBranch"
    }
}

Write-Ok "Done. Repository root: $repoRoot"
