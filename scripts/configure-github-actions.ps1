# ============================================================
# Configure GitHub Actions — Variables & Secrets
# ============================================================
# Sets repository variables (non-sensitive) via the GitHub REST
# API and repository secrets (sensitive) via the GitHub REST API
# using libsodium-sealed-box encryption required by the API.
#
# Encryption strategy (no external tools required):
#   1. Download Sodium.Core NuGet package to a temp directory.
#   2. Load the managed DLL with Add-Type and call
#      Sodium.SealedPublicKeyBox.Create() to encrypt each secret.
#   3. If Sodium.Core cannot be loaded, fall back to gh CLI
#      (if installed) or print manual-setup instructions.
#
# Usage:
#   .\configure-github-actions.ps1 `
#       -GithubToken  $env:GITHUB_TOKEN `
#       -DockerUsername  myuser `
#       -DockerPassword  mytoken `
#       -KubeConfigStagingBase64  (Get-Content staging.kubeconfig | base64) `
#       -KubeConfigProdBase64     (Get-Content prod.kubeconfig    | base64)

param(
    [Parameter(Mandatory = $true)]
    [string]$GithubToken,

    [Parameter(Mandatory = $false)]
    [string]$RepoOwner = "WolfManager",

    [Parameter(Mandatory = $false)]
    [string]$RepoName = "WolfBlockchain",

    [Parameter(Mandatory = $false)]
    [string]$StagingNamespace = "wolf-blockchain",

    [Parameter(Mandatory = $false)]
    [string]$ProductionNamespace = "wolf-blockchain",

    [Parameter(Mandatory = $false)]
    [string]$DockerUsername,

    [Parameter(Mandatory = $false)]
    [string]$DockerPassword,

    [Parameter(Mandatory = $false)]
    [string]$KubeConfigStagingBase64,

    [Parameter(Mandatory = $false)]
    [string]$KubeConfigProdBase64,

    [Parameter(Mandatory = $false)]
    [string]$SlackWebhook
)

$ErrorActionPreference = "Stop"

$SodiumCoreVersion = "1.3.4"

function Write-Info($m)  { Write-Host "ℹ️  $m" -ForegroundColor Cyan }
function Write-Ok($m)    { Write-Host "✅ $m" -ForegroundColor Green }
function Write-Warn($m)  { Write-Host "⚠️  $m" -ForegroundColor Yellow }
function Write-Err($m)   { Write-Host "❌ $m" -ForegroundColor Red }

$repo = "$RepoOwner/$RepoName"
$apiHeaders = @{
    "Accept"               = "application/vnd.github+json"
    "Authorization"        = "Bearer $GithubToken"
    "X-GitHub-Api-Version" = "2022-11-28"
}

# ------------------------------------------------------------------
# Upsert a repository variable (non-sensitive, stored in plain text)
# ------------------------------------------------------------------
function Set-RepoVariable {
    param([string]$Name, [string]$Value)

    $body = @{ name = $Name; value = $Value } | ConvertTo-Json

    try {
        Invoke-RestMethod -Method Patch `
            -Uri "https://api.github.com/repos/$repo/actions/variables/$Name" `
            -Headers $apiHeaders -ContentType "application/json" -Body $body | Out-Null
        Write-Ok "Updated variable: $Name"
    }
    catch {
        if ($_.Exception.Response.StatusCode.value__ -eq 404) {
            Invoke-RestMethod -Method Post `
                -Uri "https://api.github.com/repos/$repo/actions/variables" `
                -Headers $apiHeaders -ContentType "application/json" -Body $body | Out-Null
            Write-Ok "Created variable: $Name"
        }
        else { throw }
    }
}

# ------------------------------------------------------------------
# Load Sodium.Core from a local NuGet cache (downloaded on demand)
# ------------------------------------------------------------------
$script:SodiumLoaded = $false

function Initialize-Sodium {
    if ($script:SodiumLoaded) { return $true }

    $cacheDir  = Join-Path ([System.IO.Path]::GetTempPath()) "WolfBlockchain-Sodium"
    $markerDll = Join-Path $cacheDir "Sodium.Core.dll"

    if (-not (Test-Path $markerDll)) {
        Write-Info "Downloading Sodium.Core NuGet package for secret encryption …"
        New-Item -ItemType Directory -Force -Path $cacheDir | Out-Null

        $nupkgPath = Join-Path $cacheDir "sodium.core.nupkg"
        try {
            Invoke-WebRequest `
                -Uri "https://api.nuget.org/v3-flatcontainer/sodium.core/$SodiumCoreVersion/sodium.core.$SodiumCoreVersion.nupkg" `
                -OutFile $nupkgPath -UseBasicParsing
        }
        catch {
            Write-Warn "Could not download Sodium.Core: $_"
            return $false
        }

        Expand-Archive -Path $nupkgPath -DestinationPath $cacheDir -Force

        # Prefer net8 / net6 / netstandard DLL in that order
        $dll = Get-ChildItem -Path $cacheDir -Filter "Sodium.Core.dll" -Recurse |
               Sort-Object { $_.FullName -match 'net8' ? 0 : ($_.FullName -match 'net6' ? 1 : 2) } |
               Select-Object -First 1

        if ($null -eq $dll) {
            Write-Warn "Sodium.Core.dll not found after extraction."
            return $false
        }

        Copy-Item $dll.FullName $markerDll -Force
    }

    try {
        Add-Type -Path $markerDll -ErrorAction Stop
        $script:SodiumLoaded = $true
        return $true
    }
    catch {
        Write-Warn "Could not load Sodium.Core.dll: $_"
        return $false
    }
}

# ------------------------------------------------------------------
# Encrypt a secret value using the repository's Curve25519 public key
# (GitHub Secrets API requires libsodium crypto_box_seal).
# Returns $null when encryption is not possible.
# ------------------------------------------------------------------
function ConvertTo-SealedSecret {
    param([string]$PlainText, [string]$PublicKeyBase64)

    if (-not (Initialize-Sodium)) { return $null }

    try {
        $messageBytes   = [System.Text.Encoding]::UTF8.GetBytes($PlainText)
        $publicKeyBytes = [Convert]::FromBase64String($PublicKeyBase64)
        $sealed         = [Sodium.SealedPublicKeyBox]::Create($messageBytes, $publicKeyBytes)
        return [Convert]::ToBase64String($sealed)
    }
    catch {
        Write-Warn "Sodium encryption failed: $_"
        return $null
    }
}

# ------------------------------------------------------------------
# Set a repository secret via the GitHub REST API.
# Falls back to gh CLI when Sodium.Core cannot be loaded.
# Prints manual instructions when neither approach is available.
# ------------------------------------------------------------------
function Set-RepoSecret {
    param([string]$Name, [string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        Write-Warn "Skipping empty secret: $Name"
        return
    }

    # ── Approach 1: GitHub REST API with Sodium encryption ──────────
    try {
        $pkResponse = Invoke-RestMethod `
            -Uri "https://api.github.com/repos/$repo/actions/secrets/public-key" `
            -Headers $apiHeaders -Method Get

        $encrypted = ConvertTo-SealedSecret -PlainText $Value -PublicKeyBase64 $pkResponse.key

        if ($null -ne $encrypted) {
            $body = @{
                encrypted_value = $encrypted
                key_id          = $pkResponse.key_id
            } | ConvertTo-Json

            Invoke-RestMethod -Method Put `
                -Uri "https://api.github.com/repos/$repo/actions/secrets/$Name" `
                -Headers $apiHeaders -ContentType "application/json" -Body $body | Out-Null

            Write-Ok "Set secret via API: $Name"
            return
        }
    }
    catch {
        Write-Warn "GitHub API secret set failed for $Name`: $_"
    }

    # ── Approach 2: gh CLI ───────────────────────────────────────────
    if (Get-Command gh -ErrorAction SilentlyContinue) {
        $env:GH_TOKEN = $GithubToken
        # Write value to a temp file so that binary/multiline content
        # (e.g. base64-encoded kubeconfigs) is transmitted without shell
        # escaping or pipeline truncation issues.
        $tmpFile = [System.IO.Path]::GetTempFileName()
        try {
            [System.IO.File]::WriteAllText($tmpFile, $Value, [System.Text.Encoding]::UTF8)
            gh secret set $Name --body (Get-Content -Raw $tmpFile) -R $repo
        }
        finally {
            Remove-Item $tmpFile -ErrorAction SilentlyContinue
        }
        if ($LASTEXITCODE -eq 0) {
            Write-Ok "Set secret via gh CLI: $Name"
            return
        }
        Write-Warn "gh CLI returned exit code $LASTEXITCODE for secret $Name."
    }

    # ── Approach 3: Manual instructions ─────────────────────────────
    Write-Err "Could not set secret '$Name' automatically."
    Write-Host "   Please set it manually:" -ForegroundColor Yellow
    Write-Host "   GitHub UI → https://github.com/$repo/settings/secrets/actions/new" -ForegroundColor Yellow
    Write-Host "   Name: $Name" -ForegroundColor Yellow
    Write-Host "   Or install gh CLI: https://cli.github.com  then re-run this script." -ForegroundColor Yellow
}

# ==================================================================
# Main
# ==================================================================
Write-Info "Configuring GitHub Actions for repository: $repo"

Write-Info "Setting repository variables …"
Set-RepoVariable -Name "STAGING_NAMESPACE"    -Value $StagingNamespace
Set-RepoVariable -Name "PRODUCTION_NAMESPACE" -Value $ProductionNamespace

Write-Info "Setting repository secrets …"
Set-RepoSecret -Name "DOCKER_USERNAME"      -Value $DockerUsername
Set-RepoSecret -Name "DOCKER_PASSWORD"      -Value $DockerPassword
Set-RepoSecret -Name "KUBE_CONFIG_STAGING"  -Value $KubeConfigStagingBase64
Set-RepoSecret -Name "KUBE_CONFIG_PROD"     -Value $KubeConfigProdBase64
if (-not [string]::IsNullOrWhiteSpace($SlackWebhook)) {
    Set-RepoSecret -Name "SLACK_WEBHOOK" -Value $SlackWebhook
}

Write-Ok "GitHub Actions configuration completed for $repo"
Write-Host ""
Write-Info "Next steps:"
Write-Host "  1. Verify secrets at: https://github.com/$repo/settings/secrets/actions" -ForegroundColor Cyan
Write-Host "  2. Set Kubernetes cluster secrets (wolf-blockchain-secrets) directly in each cluster." -ForegroundColor Cyan
Write-Host "     See k8s/03-secret.yaml for the required keys." -ForegroundColor Cyan
Write-Host "  3. Push to main to trigger CI, or run the deploy workflow manually." -ForegroundColor Cyan