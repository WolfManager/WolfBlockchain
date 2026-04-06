# =============================================================================
# 🐺 WolfBlockchain - Generate All Necessary Project Files
# =============================================================================
# Usage:
#   .\generate-project-files.ps1
#   .\generate-project-files.ps1 -Force          # overwrite existing files
#   .\generate-project-files.ps1 -WhatIf         # preview without writing
#   .\generate-project-files.ps1 -Only workflow  # generate only one category
#
# Categories: workflow, appsettings, docker, env, docs, k8s
# =============================================================================

param(
    [switch]$Force,
    [switch]$WhatIf,
    [ValidateSet("", "workflow", "appsettings", "docker", "env", "docs", "k8s")]
    [string]$Only = ""
)

$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------
function Write-Ok($msg)   { Write-Host "✅ $msg" -ForegroundColor Green }
function Write-Skip($msg) { Write-Host "⏭  $msg" -ForegroundColor DarkGray }
function Write-Info($msg) { Write-Host "ℹ️  $msg" -ForegroundColor Cyan }
function Write-Warn($msg) { Write-Host "⚠️  $msg" -ForegroundColor Yellow }

$createdCount = 0
$skippedCount = 0

function Write-File {
    param(
        [string]$RelativePath,
        [string]$Content,
        [string]$Category
    )

    if ($Only -ne "" -and $Category -ne $Only) { return }

    $fullPath = Join-Path $repoRoot $RelativePath

    if ((Test-Path $fullPath) -and -not $Force) {
        Write-Skip "Already exists: $RelativePath"
        $script:skippedCount++
        return
    }

    $dir = Split-Path $fullPath -Parent
    if (-not (Test-Path $dir)) {
        if ($WhatIf) {
            Write-Info "[WhatIf] Would create directory: $dir"
        } else {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
        }
    }

    if ($WhatIf) {
        Write-Info "[WhatIf] Would write: $RelativePath"
    } else {
        Set-Content -Path $fullPath -Value $Content -NoNewline -Encoding UTF8
        Write-Ok "Created: $RelativePath"
    }
    $script:createdCount++
}

# ---------------------------------------------------------------------------
# Repo root detection
# ---------------------------------------------------------------------------
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot   = Resolve-Path (Join-Path $scriptDir "..")

Write-Host ""
Write-Host "🐺 WolfBlockchain — Generate Project Files" -ForegroundColor Cyan
Write-Host "   Repo root : $repoRoot"
Write-Host "   Force     : $Force"
Write-Host "   WhatIf    : $WhatIf"
if ($Only) { Write-Host "   Only      : $Only" }
Write-Host ""

# =============================================================================
# CATEGORY: workflow
# =============================================================================

Write-File -Category "workflow" -RelativePath ".github/workflows/deploy.yml" -Content @'
# =============================================================================
# 🐺 WolfBlockchain — GitHub Actions CI/CD Pipeline
# =============================================================================
# Required repository variables  : STAGING_NAMESPACE, PRODUCTION_NAMESPACE
# Required repository secrets    : DOCKER_USERNAME, DOCKER_PASSWORD,
#                                  KUBE_CONFIG_STAGING, KUBE_CONFIG_PROD
# Optional repository secrets    : SLACK_WEBHOOK
# =============================================================================

name: CI/CD Pipeline

on:
  push:
    branches: [ main, staging ]
  pull_request:
    branches: [ main ]

env:
  DOTNET_VERSION: "10.0.x"
  IMAGE_NAME: wolfblockchain

jobs:
  # ---------------------------------------------------------------------------
  # build-and-test
  # ---------------------------------------------------------------------------
  build-and-test:
    name: Build & Test
    runs-on: ubuntu-latest
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build -c Release --no-restore

      - name: Run unit tests
        run: >
          dotnet test tests/WolfBlockchain.Tests/WolfBlockchain.Tests.csproj
          -c Release --no-build
          --logger "trx;LogFileName=test-results.trx"
          --results-directory ./test-results

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: test-results
          path: ./test-results

  # ---------------------------------------------------------------------------
  # security-scan
  # ---------------------------------------------------------------------------
  security-scan:
    name: Security Scan
    runs-on: ubuntu-latest
    needs: build-and-test
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Run Trivy vulnerability scanner (filesystem)
        uses: aquasecurity/trivy-action@master
        with:
          scan-type: "fs"
          scan-ref: "."
          format: "sarif"
          output: "trivy-results.sarif"
          severity: "CRITICAL,HIGH"
        continue-on-error: true

      - name: Upload Trivy SARIF results
        if: always()
        uses: github/codeql-action/upload-sarif@v3
        with:
          sarif_file: "trivy-results.sarif"
        continue-on-error: true

  # ---------------------------------------------------------------------------
  # docker-build
  # ---------------------------------------------------------------------------
  docker-build:
    name: Docker Build & Push
    runs-on: ubuntu-latest
    needs: build-and-test
    if: github.event_name == 'push'
    outputs:
      image-tag: ${{ steps.meta.outputs.tags }}
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Docker meta
        id: meta
        uses: docker/metadata-action@v5
        with:
          images: ${{ secrets.DOCKER_USERNAME }}/${{ env.IMAGE_NAME }}
          tags: |
            type=ref,event=branch
            type=sha,prefix=sha-

      - name: Login to Docker Hub
        uses: docker/login-action@v3
        with:
          username: ${{ secrets.DOCKER_USERNAME }}
          password: ${{ secrets.DOCKER_PASSWORD }}

      - name: Build and push Docker image
        uses: docker/build-push-action@v5
        with:
          context: .
          push: true
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
          cache-from: type=gha
          cache-to: type=gha,mode=max

  # ---------------------------------------------------------------------------
  # deploy-staging
  # ---------------------------------------------------------------------------
  deploy-staging:
    name: Deploy to Staging
    runs-on: ubuntu-latest
    needs: docker-build
    if: github.ref == 'refs/heads/staging'
    environment:
      name: staging
      url: https://staging.wolf-blockchain.example.com
    env:
      STAGING_NAMESPACE: ${{ vars.STAGING_NAMESPACE }}
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Configure kubectl (staging)
        run: |
          mkdir -p $HOME/.kube
          echo "${{ secrets.KUBE_CONFIG_STAGING }}" | base64 --decode > $HOME/.kube/config
          chmod 600 $HOME/.kube/config

      - name: Deploy to staging namespace
        run: |
          IMAGE_TAG="${{ needs.docker-build.outputs.image-tag }}"
          kubectl set image deployment/wolf-blockchain-api \
            api="${IMAGE_TAG}" \
            -n "${STAGING_NAMESPACE}"
          kubectl rollout status deployment/wolf-blockchain-api \
            -n "${STAGING_NAMESPACE}" \
            --timeout=300s

      - name: Run smoke tests
        run: bash scripts/smoke-tests.sh http://staging.wolf-blockchain.example.com 5

      - name: Notify Slack (staging)
        if: always() && secrets.SLACK_WEBHOOK
        uses: slackapi/slack-github-action@v1.26.0
        with:
          payload: |
            {
              "text": "Staging deploy ${{ job.status == 'success' && '✅ succeeded' || '❌ failed' }} — ${{ github.sha }}"
            }
        env:
          SLACK_WEBHOOK_URL: ${{ secrets.SLACK_WEBHOOK }}
          SLACK_WEBHOOK_TYPE: INCOMING_WEBHOOK

  # ---------------------------------------------------------------------------
  # deploy-production
  # ---------------------------------------------------------------------------
  deploy-production:
    name: Deploy to Production
    runs-on: ubuntu-latest
    needs: docker-build
    if: github.ref == 'refs/heads/main'
    environment:
      name: production
      url: https://wolf-blockchain.example.com
    env:
      PRODUCTION_NAMESPACE: ${{ vars.PRODUCTION_NAMESPACE }}
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Configure kubectl (production)
        run: |
          mkdir -p $HOME/.kube
          echo "${{ secrets.KUBE_CONFIG_PROD }}" | base64 --decode > $HOME/.kube/config
          chmod 600 $HOME/.kube/config

      - name: Deploy to production namespace
        run: |
          IMAGE_TAG="${{ needs.docker-build.outputs.image-tag }}"
          kubectl set image deployment/wolf-blockchain-api \
            api="${IMAGE_TAG}" \
            -n "${PRODUCTION_NAMESPACE}"
          kubectl rollout status deployment/wolf-blockchain-api \
            -n "${PRODUCTION_NAMESPACE}" \
            --timeout=300s

      - name: Run smoke tests (production)
        run: bash scripts/smoke-tests.sh https://wolf-blockchain.example.com 5

      - name: Notify Slack (production)
        if: always() && secrets.SLACK_WEBHOOK
        uses: slackapi/slack-github-action@v1.26.0
        with:
          payload: |
            {
              "text": "Production deploy ${{ job.status == 'success' && '✅ succeeded' || '❌ failed' }} — ${{ github.sha }}"
            }
        env:
          SLACK_WEBHOOK_URL: ${{ secrets.SLACK_WEBHOOK }}
          SLACK_WEBHOOK_TYPE: INCOMING_WEBHOOK
'@

# =============================================================================
# CATEGORY: appsettings
# =============================================================================

Write-File -Category "appsettings" -RelativePath "src/WolfBlockchain.API/appsettings.Development.json" -Content @'
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=WolfBlockchainDb;User=sa;Password=__SET_LOCAL_DEV_PASSWORD__;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "ExpirationMinutes": 1440,
    "RefreshTokenExpirationDays": 7
  },
  "Security": {
    "SingleAdminMode": true,
    "MaxFailedLoginAttempts": 10,
    "LoginLockoutMinutes": 5,
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:5000",
      "https://localhost:5001"
    ]
  },
  "Cache": {
    "DefaultTtlMinutes": 1,
    "SummaryTtlMinutes": 1,
    "ListTtlMinutes": 2
  },
  "RateLimit": {
    "RequestsPerMinute": 1000,
    "BurstSize": 100
  },
  "Monitoring": {
    "EnableMetrics": true,
    "EnableDetailedLogging": true,
    "LogRetentionDays": 1
  }
}
'@

Write-File -Category "appsettings" -RelativePath "src/WolfBlockchain.API/appsettings.Production.json" -Content @'
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "Security": {
    "SingleAdminMode": true,
    "MaxFailedLoginAttempts": 5,
    "LoginLockoutMinutes": 30,
    "AllowedOrigins": []
  },
  "Cache": {
    "DefaultTtlMinutes": 5,
    "SummaryTtlMinutes": 5,
    "ListTtlMinutes": 10
  },
  "RateLimit": {
    "RequestsPerMinute": 100,
    "BurstSize": 10
  },
  "Monitoring": {
    "EnableMetrics": true,
    "EnableDetailedLogging": false,
    "LogRetentionDays": 7
  }
}
'@

# =============================================================================
# CATEGORY: docker
# =============================================================================

Write-File -Category "docker" -RelativePath "docker-compose.override.yml" -Content @'
# =============================================================================
# Docker Compose Override — Development
# =============================================================================
# This file extends docker-compose.yml with development-specific overrides.
# It is automatically picked up by `docker compose up` when present.
# Do NOT commit secrets into this file.
# =============================================================================

version: '3.8'

services:
  api:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:5000
    volumes:
      - ./src:/src:ro
    ports:
      - "5000:5000"

  db:
    ports:
      - "1433:1433"
'@

# =============================================================================
# CATEGORY: env
# =============================================================================

Write-File -Category "env" -RelativePath ".env.example" -Content @'
# =============================================================================
# WolfBlockchain — Example Environment Variables
# =============================================================================
# Copy this file to .env and fill in real values.
# NEVER commit .env with real secrets.
# =============================================================================

# ---- JWT ----
# Must be at least 32 characters.
# Generate: [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes([Guid]::NewGuid().ToString() + [Guid]::NewGuid().ToString()))
Jwt__Secret=CHANGE_ME_MINIMUM_32_CHARACTERS_LONG

# ---- Database ----
ConnectionStrings__DefaultConnection=Server=localhost,1433;Database=WolfBlockchainDb;User=sa;Password=CHANGE_ME!;TrustServerCertificate=True;MultipleActiveResultSets=true
SA_PASSWORD=CHANGE_ME!

# ---- Security ----
Security__BootstrapToken=CHANGE_ME_BOOTSTRAP_TOKEN

# ---- RPC (optional) ----
RPC_PRIMARY=https://rpc.example.com
RPC_FALLBACK=https://rpc-fallback.example.com
RPC_AUTH_TOKEN=CHANGE_ME_RPC_TOKEN

# ---- Docker Hub ----
DOCKER_USERNAME=your-dockerhub-username
DOCKER_PASSWORD=your-dockerhub-password-or-token

# ---- Kubernetes ----
STAGING_NAMESPACE=wolf-blockchain
PRODUCTION_NAMESPACE=wolf-blockchain
# Base64-encoded kubeconfig files:
# KUBE_CONFIG_STAGING=<base64>
# KUBE_CONFIG_PROD=<base64>

# ---- Notifications (optional) ----
# SLACK_WEBHOOK=https://hooks.slack.com/services/...
'@

# =============================================================================
# CATEGORY: docs
# =============================================================================

Write-File -Category "docs" -RelativePath "docs/QUICK_START.md" -Content @'
# Quick Start — WolfBlockchain

## Prerequisites

| Tool | Minimum version |
|------|----------------|
| .NET SDK | 10.0 |
| Docker Desktop | 24+ |
| kubectl | 1.28+ |
| PowerShell | 7.2+ |

## 1 — Clone and set up secrets

```bash
git clone https://github.com/WolfManager/WolfBlockchain.git
cd WolfBlockchain
```

Set the JWT secret for local development (required):

```powershell
dotnet user-secrets init --project src/WolfBlockchain.API
dotnet user-secrets set "Jwt:Secret" "$(New-Guid)$(New-Guid)" --project src/WolfBlockchain.API
```

## 2 — Run with Docker Compose

```bash
docker compose up --build
```

The API will be available at <http://localhost:5000>.  
Swagger UI: <http://localhost:5000/swagger>

## 3 — Run tests

```powershell
dotnet test tests/WolfBlockchain.Tests/WolfBlockchain.Tests.csproj -c Release
```

## 4 — Deploy to staging / production

```powershell
# Configure GitHub Actions variables and secrets:
.\scripts\configure-github-actions.ps1 -GithubToken $env:GH_TOKEN

# Trigger the staging pipeline:
.\scripts\push-main-staging.ps1

# Validate staging:
.\scripts\staging-validate.ps1

# Promote to production:
.\scripts\production-promote.ps1
```

## 5 — Regenerate project files

If any scaffolded file is missing, run:

```powershell
.\scripts\generate-project-files.ps1
```

Use `-Force` to overwrite existing files, `-WhatIf` to preview without writing.
'@

# =============================================================================
# CATEGORY: k8s
# =============================================================================

Write-File -Category "k8s" -RelativePath "k8s/00-prerequisites.md" -Content @'
# Kubernetes Prerequisites

Before applying the manifests in this directory, ensure the following are in place.

## Required tools

- `kubectl` >= 1.28 connected to the target cluster
- `helm` >= 3.12 (for cert-manager / ingress-nginx installation)

## Ingress controller

```bash
helm upgrade --install ingress-nginx ingress-nginx \
  --repo https://kubernetes.github.io/ingress-nginx \
  --namespace ingress-nginx --create-namespace
```

## cert-manager

```bash
helm upgrade --install cert-manager cert-manager \
  --repo https://charts.jetstack.io \
  --namespace cert-manager --create-namespace \
  --set installCRDs=true
```

## Apply manifests in order

```bash
kubectl apply -f k8s/01-namespace.yaml
kubectl apply -f k8s/02-configmap.yaml
kubectl apply -f k8s/03-secret.yaml   # fill real secrets first!
kubectl apply -f k8s/04-pvc.yaml
kubectl apply -f k8s/05-statefulset-db.yaml
kubectl apply -f k8s/06-services.yaml
kubectl apply -f k8s/07-deployment.yaml
kubectl apply -f k8s/08-hpa.yaml
kubectl apply -f k8s/09-ingress.yaml
kubectl apply -f k8s/10-prometheus-config.yaml
kubectl apply -f k8s/11-prometheus-deployment.yaml
```

> ⚠️ Edit `k8s/03-secret.yaml` and replace every `__SET_IN_CLUSTER_ONLY__`
> placeholder with real base64-encoded values before applying.
'@

# =============================================================================
# Summary
# =============================================================================

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Summary" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
if ($WhatIf) {
    Write-Host "  [WhatIf mode — no files were written]" -ForegroundColor Yellow
} else {
    Write-Host "  Created : $createdCount file(s)" -ForegroundColor Green
    Write-Host "  Skipped : $skippedCount file(s) (already exist)" -ForegroundColor DarkGray
}
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

if (-not $WhatIf -and $createdCount -eq 0 -and $skippedCount -gt 0) {
    Write-Warn "All files already exist. Use -Force to overwrite."
}
