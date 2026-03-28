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

function Write-Info($m) { Write-Host "ℹ️  $m" -ForegroundColor Cyan }
function Write-Ok($m) { Write-Host "✅ $m" -ForegroundColor Green }
function Write-Warn($m) { Write-Host "⚠️  $m" -ForegroundColor Yellow }

$repo = "$RepoOwner/$RepoName"
$headers = @{
    "Accept"               = "application/vnd.github+json"
    "Authorization"        = "Bearer $GithubToken"
    "X-GitHub-Api-Version" = "2022-11-28"
}

function Set-RepoVariable {
    param(
        [string]$Name,
        [string]$Value
    )

    $body = @{ name = $Name; value = $Value } | ConvertTo-Json

    try {
        Invoke-RestMethod -Method Patch -Uri "https://api.github.com/repos/$repo/actions/variables/$Name" -Headers $headers -ContentType "application/json" -Body $body | Out-Null
        Write-Ok "Updated variable: $Name"
    }
    catch {
        if ($_.Exception.Message -like "*404*") {
            Invoke-RestMethod -Method Post -Uri "https://api.github.com/repos/$repo/actions/variables" -Headers $headers -ContentType "application/json" -Body $body | Out-Null
            Write-Ok "Created variable: $Name"
        }
        else {
            throw
        }
    }
}

function Set-RepoSecret {
    param(
        [string]$Name,
        [string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        Write-Warn "Skipping empty secret value: $Name"
        return
    }

    $tempPath = Join-Path $env:TEMP ("gh-secret-{0}.txt" -f $Name)
    Set-Content -Path $tempPath -Value $Value -NoNewline

    try {
        cmd /c "echo $Value | gh secret set $Name -R $repo" | Out-Null
        Write-Ok "Set secret: $Name"
    }
    finally {
        Remove-Item $tempPath -ErrorAction SilentlyContinue
    }
}

Write-Info "Configuring GitHub Actions variables for $repo"
Set-RepoVariable -Name "STAGING_NAMESPACE" -Value $StagingNamespace
Set-RepoVariable -Name "PRODUCTION_NAMESPACE" -Value $ProductionNamespace

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Warn "GitHub CLI (gh) is not installed. Variables are configured via API; secrets must be configured manually in GitHub UI or after installing gh."
    exit 0
}

$env:GH_TOKEN = $GithubToken

Write-Info "Configuring GitHub Actions secrets for $repo"
Set-RepoSecret -Name "DOCKER_USERNAME" -Value $DockerUsername
Set-RepoSecret -Name "DOCKER_PASSWORD" -Value $DockerPassword
Set-RepoSecret -Name "KUBE_CONFIG_STAGING" -Value $KubeConfigStagingBase64
Set-RepoSecret -Name "KUBE_CONFIG_PROD" -Value $KubeConfigProdBase64
Set-RepoSecret -Name "SLACK_WEBHOOK" -Value $SlackWebhook

Write-Ok "GitHub Actions configuration completed for $repo"