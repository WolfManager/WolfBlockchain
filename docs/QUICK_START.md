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