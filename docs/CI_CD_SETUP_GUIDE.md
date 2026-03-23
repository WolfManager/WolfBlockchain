# CI/CD Pipeline Configuration Guide

## Overview
This document explains how to configure the CI/CD pipeline for WolfBlockchain using GitHub Actions.

---

## Prerequisites

### 1. GitHub Repository
- Your code must be in a GitHub repository
- Branch structure: `main` (production), `staging`, `develop`

### 2. Docker Registry Account
- Docker Hub account or private registry
- Username and password/token for authentication

### 3. Kubernetes Cluster
- Access to Kubernetes cluster (staging + production)
- `kubeconfig` files for both environments

### 4. Slack Workspace (Optional)
- Slack workspace with webhook for notifications
- Create incoming webhook for notifications

---

## Setup Instructions

### Step 1: Create GitHub Secrets

Go to your GitHub repository → **Settings** → **Secrets and variables** → **Actions**

#### Required Secrets

**1. Docker Credentials**
```
DOCKER_USERNAME = your-docker-username
DOCKER_PASSWORD = your-docker-password-or-token
```

**2. Kubernetes Staging Config**
```
KUBE_CONFIG_STAGING = <base64-encoded-kubeconfig>
```

To encode kubeconfig:
```bash
cat ~/.kube/config-staging | base64 -w 0
```

**3. Kubernetes Production Config**
```
KUBE_CONFIG_PROD = <base64-encoded-kubeconfig>
```

**4. Slack Webhook (Optional)**
```
SLACK_WEBHOOK = https://hooks.slack.com/services/YOUR/WEBHOOK/URL
```

### Step 2: Configure Branch Protection Rules

Go to **Settings** → **Branches** → **Add rule**

**For `main` branch (production):**
- ✅ Require pull request reviews (at least 1)
- ✅ Require status checks to pass
- ✅ Require branches to be up to date
- ✅ Restrict who can push (optional, for high security)

**For `staging` branch:**
- ✅ Require status checks to pass
- ☐ Allow direct pushes (for faster iteration)

### Step 3: Enable Environments

Go to **Settings** → **Environments** → **New environment**

**Create `staging` environment:**
- ✅ Required reviewers (optional)
- Set deployment branches to: `ref:refs/heads/staging`

**Create `production` environment:**
- ✅ Required reviewers (at least 1, strongly recommended)
- Set deployment branches to: `ref:refs/heads/main`
- ✅ Prevent deployment of inactive branches (30 days)

---

## Workflow Overview

### On Every Push

```
Code pushed to GitHub
    ↓
1. Build & Test (all branches)
   - Restore dependencies
   - Build solution
   - Run unit tests
   - Upload test results
    ↓
2. Docker Build & Push (main/staging/develop only)
   - Build Docker image
   - Push to registry with tags
    ↓
3. Deploy to Environment (staging or production)
   - Update K8s deployment
   - Wait for rollout
   - Run smoke tests
   - Notify Slack
```

### Branch Triggers

| Branch | Trigger | Deployment | Notification |
|--------|---------|-----------|--------------|
| `develop` | On push | None | Test results |
| `staging` | On push | Staging K8s | Slack webhook |
| `main` | On push | Production K8s | Slack webhook |
| `feature/*` | On PR | None | Test results |

---

## Manual Deployment (Without CI/CD)

If you need to deploy manually:

### PowerShell (Windows)
```powershell
.\scripts\deploy.ps1 -Environment staging -Version v2.0.1 -DockerImage wolfblockchain:v2.0.1
```

### Bash (macOS/Linux)
```bash
bash scripts/deploy.sh staging v2.0.1 wolfblockchain:v2.0.1
```

---

## Monitoring Deployments

### GitHub Actions Dashboard
1. Go to your repository
2. Click **Actions** tab
3. View workflow runs, logs, and results

### Slack Notifications
The workflow posts updates to your configured Slack webhook:
- ✅ Build success
- ❌ Build failure
- 🚀 Deployment started
- ✅ Deployment success
- ❌ Deployment failure

### Kubernetes Rollout Status
```bash
# Watch staging deployment
kubectl rollout status deployment/wolf-blockchain-api -n wolf-blockchain-staging -w

# Watch production deployment
kubectl rollout status deployment/wolf-blockchain-api -n wolf-blockchain-prod -w
```

---

## Rollback Procedures

### Automatic Rollback (Not Configured)
The current workflow does NOT auto-rollback on failure. This is intentional to prevent cascading failures.

### Manual Rollback

#### Kubernetes Rollback
```bash
# View rollout history
kubectl rollout history deployment/wolf-blockchain-api -n wolf-blockchain-prod

# Rollback to previous version
kubectl rollout undo deployment/wolf-blockchain-api -n wolf-blockchain-prod

# Rollback to specific revision
kubectl rollout undo deployment/wolf-blockchain-api -n wolf-blockchain-prod --to-revision=2
```

#### Via GitHub Actions
1. Go to **Actions** → Previous successful run
2. Click **Re-run failed jobs** or **Re-run all jobs**
3. This redeploys the previous stable version

---

## Troubleshooting

### Test Failures
```bash
# Run tests locally to debug
dotnet test tests\WolfBlockchain.Tests\WolfBlockchain.Tests.csproj -c Release

# Run specific test
dotnet test --filter "FullyQualifiedName~TokenManagement"
```

### Docker Build Failures
```bash
# Build locally to debug
docker build -t wolfblockchain:test .

# Check Docker logs
docker logs <container-id>
```

### Kubernetes Deployment Issues
```bash
# Check deployment status
kubectl describe deployment wolf-blockchain-api -n wolf-blockchain-staging

# View pod logs
kubectl logs -f deployment/wolf-blockchain-api -n wolf-blockchain-staging

# Check pod events
kubectl get events -n wolf-blockchain-staging --sort-by='.lastTimestamp'
```

### Secrets Not Found
```bash
# Verify secret is set in GitHub
gh secret list

# Verify secret is available in workflow
# Check "Actions secrets" in repository settings
```

---

## Security Best Practices

### 1. Credentials
- ✅ Use GitHub Secrets (never commit credentials)
- ✅ Rotate secrets quarterly
- ✅ Use read-only registry tokens for pulls
- ✅ Use write tokens only for pushes

### 2. Access Control
- ✅ Require PR reviews for production
- ✅ Use protected branches
- ✅ Limit who can approve deployments
- ✅ Audit all deployments

### 3. Image Security
- ✅ Scan images with Trivy (already in workflow)
- ✅ Use specific versions (not `latest`)
- ✅ Sign images (future enhancement)
- ✅ Private registry (future)

### 4. Kubernetes Security
- ✅ Use RBAC for deployments
- ✅ Rotate kubeconfig files
- ✅ Limit deployment permissions
- ✅ Enable audit logging

---

## Performance Optimization

### Cache Docker Layers
The workflow caches Docker build layers to speed up subsequent builds.
- First build: ~2-3 minutes
- Subsequent builds: ~30-60 seconds

### Parallel Jobs
Some jobs run in parallel to save time:
- Build & Test: Always
- Docker Build: Only after Build & Test passes
- Deploy: Only if Docker Build succeeds

---

## Next Steps

1. ✅ Create GitHub secrets (see Step 1 above)
2. ✅ Configure branch protection rules
3. ✅ Set up Slack webhook (optional)
4. ✅ Push code to trigger first workflow
5. ✅ Monitor Actions dashboard
6. ✅ Celebrate! 🎉

---

## Support & Troubleshooting

**Contact**: [DevOps Team Contact]
**Documentation**: See DEPLOYMENT_RUNBOOK.md
**Status Page**: [Status Page URL]

---

**Last Updated**: 2026-03-22
**Version**: 1.0.0
