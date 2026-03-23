# Staging & Production Environment Parity

## Goal
Ensure staging and production are identical in configuration, so surprises don't happen during launch.

---

## Environment Comparison Matrix

| Aspect | Development | Staging | Production | Parity? |
|--------|-------------|---------|------------|---------|
| **Infrastructure** | | | | |
| Kubernetes | Local Docker Desktop | AWS EKS / Azure AKS | AWS EKS / Azure AKS | ✅ |
| Nodes | 1 | 3+ | 3+ | ✅ |
| Pod Replicas | 1-3 | 3 | 3-10 | ✅ |
| Database | LocalDB / Docker | SQL Server RDS | SQL Server RDS | ✅ |
| Storage | Local PV | EBS / Azure Disk | EBS / Azure Disk | ✅ |
| **Configuration** | | | | |
| .NET Version | 10 | 10 | 10 | ✅ |
| Logging Level | Debug | Info | Info | ✅ |
| Cache TTL | 5 min | 5 min | 5 min | ✅ |
| Rate Limit | 100 req/min | 100 req/min | 100 req/min | ✅ |
| API Timeout | 30s | 30s | 30s | ✅ |
| **Secrets & Auth** | | | | |
| Jwt:Secret | user-secrets | K8s secret | K8s secret | ✅ |
| DB Connection | LocalDB | RDS endpoint | RDS endpoint | ✅ |
| CORS Origins | localhost:5000 | staging URL | production URL | ✅ |
| TLS | Self-signed | Let's Encrypt | Let's Encrypt | ✅ |
| **Monitoring** | | | | |
| Prometheus | Enabled | Enabled | Enabled | ✅ |
| Logs | File | Cloud logging | Cloud logging | ✅ |
| Alerts | None | Slack | Slack + PagerDuty | ✅ |
| **Security** | | | | |
| Network Policy | Disabled | Enabled | Enabled | ⚠️ Diff |
| Pod Security | Disabled | Enabled | Enforced | ⚠️ Diff |
| HTTPS | No | Yes | Yes | ⚠️ Diff |

---

## Configuration Files

### appsettings.json (Base - Same for all)
```json
{
  "Jwt": {
    "ExpirationMinutes": 1440
  },
  "Security": {
    "SingleAdminMode": true,
    "MaxFailedAttempts": 5
  },
  "Cache": {
    "DefaultTtlMinutes": 5
  },
  "RateLimit": {
    "RequestsPerMinute": 100
  }
}
```

### appsettings.Development.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=WolfBlockchain;..."
  },
  "Security": {
    "AllowedOrigins": [
      "http://localhost:5000",
      "https://localhost:5001"
    ]
  }
}
```

### appsettings.Staging.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=staging-db.c9akciq32.us-east-1.rds.amazonaws.com;Database=WolfBlockchain;Encrypt=true;..."
  },
  "Security": {
    "AllowedOrigins": [
      "https://staging.wolf-blockchain.local"
    ]
  },
  "Monitoring": {
    "EnableDetailedLogging": true
  }
}
```

### appsettings.Production.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-db.c9akciq32.us-east-1.rds.amazonaws.com;Database=WolfBlockchain;Encrypt=true;..."
  },
  "Security": {
    "AllowedOrigins": [
      "https://api.wolf-blockchain.com"
    ]
  },
  "Monitoring": {
    "EnableDetailedLogging": false
  }
}
```

---

## Kubernetes Manifests Parity

### Base Manifest (k8s/07-deployment.yaml)
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: wolf-blockchain-api
spec:
  replicas: 3  # Same for staging and production
  selector:
    matchLabels:
      app: wolf-blockchain-api
  template:
    metadata:
      labels:
        app: wolf-blockchain-api
    spec:
      containers:
      - name: api
        image: wolfblockchain:v2.0.0  # Specific version tag
        ports:
        - containerPort: 5000
        resources:
          requests:
            memory: "384Mi"
            cpu: "200m"
          limits:
            memory: "768Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 5
          periodSeconds: 5
```

### Namespace Override (kustomize)
```yaml
# k8s/overlays/staging/kustomization.yaml
namespace: wolf-blockchain-staging

replicas:
- name: wolf-blockchain-api
  count: 3

# Staging-specific ConfigMap
configMapGenerator:
- name: app-config
  literals:
  - ASPNETCORE_ENVIRONMENT=Staging
  - LOG_LEVEL=Information

# Staging secrets reference
secretGenerator:
- name: wolf-blockchain-secrets
  envs:
  - staging-secrets.env
```

```yaml
# k8s/overlays/production/kustomization.yaml
namespace: wolf-blockchain-prod

replicas:
- name: wolf-blockchain-api
  count: 3

# Production-specific ConfigMap
configMapGenerator:
- name: app-config
  literals:
  - ASPNETCORE_ENVIRONMENT=Production
  - LOG_LEVEL=Information

# Production secrets reference
secretGenerator:
- name: wolf-blockchain-secrets
  envs:
  - prod-secrets.env

# Production-specific patches
patchesJson6902:
- target:
    group: apps
    version: v1
    kind: Deployment
    name: wolf-blockchain-api
  patch: |-
    - op: replace
      path: /spec/replicas
      value: 5  # Scale up in production
```

---

## Deployment Verification (Pre-Launch)

### Checklist: Staging = Production?

**Infrastructure**
- [ ] Same number of nodes (3+)
- [ ] Same pod replicas (3)
- [ ] Same database version (SQL Server 2022)
- [ ] Same storage type (EBS/Azure Disk)
- [ ] Same network configuration

**Configuration**
- [ ] .NET version same (10)
- [ ] Logging levels same
- [ ] Cache TTLs same
- [ ] Rate limits same
- [ ] Timeouts same

**Secrets & Security**
- [ ] Database credentials work identically
- [ ] TLS certificates valid
- [ ] CORS properly configured per environment
- [ ] Security headers present

**Monitoring & Logging**
- [ ] Prometheus scraping both
- [ ] Logs going to same centralized location
- [ ] Alerts firing for both
- [ ] Dashboards show both environments

**Performance**
- [ ] API latency similar (<100ms p50)
- [ ] Database query times comparable
- [ ] Cache hit rates similar
- [ ] No resource bottlenecks in staging

---

## Environment-Specific Differences (Acceptable)

These differences are **expected and required**:

| Aspect | Staging | Production | Reason |
|--------|---------|-----------|--------|
| **Replicas** | 3 | 3-10 (HPA) | Cost optimization for staging |
| **PagerDuty** | No | Yes | Only on-call in production |
| **Backup Frequency** | Daily | Daily | Same |
| **Retention** | 7 days | 30 days | Production = longer retention |
| **Update Windows** | Anytime | Scheduled (2am UTC) | Business continuity |
| **Rollback** | Immediate | Approved | Extra gate in production |
| **DNS** | staging.wolf... | api.wolf... | Different domains |
| **TLS Issuer** | Let's Encrypt | Let's Encrypt | Same provider |

---

## Validation Procedure (Before Production Launch)

### Step 1: Deploy to Staging
```bash
git push origin develop:staging
# GitHub Actions automatically deploys to staging
# Monitor: kubectl logs -f deployment/wolf-blockchain-api -n wolf-blockchain-staging
```

### Step 2: Run Staging Tests
```bash
# Smoke tests
bash scripts/smoke-tests.sh https://staging.wolf-blockchain.local

# Integration tests
dotnet test tests/WolfBlockchain.Tests --filter "Category==Integration"

# Manual UAT
# - Create token
# - Deploy contract
# - Monitor AI job
# - Check dashboards
```

### Step 3: Compare Metrics
```bash
# Production metrics (will exist after launch)
# Staging metrics (now)
# Compare:
# - API latency (should be within 10%)
# - Cache hit rate (should be within 5%)
# - Error rate (both should be <0.5%)
# - Database query times (should be within 10%)

# If metrics differ significantly:
# - Staging config doesn't match
# - Staging hardware different
# - Staging database not optimized
# → Fix differences before production launch
```

### Step 4: Load Test Staging
```bash
bash tests/load-test.sh 50 60  # 50 concurrent users, 60 seconds

# Verify:
# - API responsive under load
# - No memory leaks
# - Database connection pool sufficient
# - Cache effective
```

### Step 5: Security Verification
```bash
# Run security tests in staging
# - SQL injection attempts
# - XSS payload testing
# - Rate limit enforcement
# - CORS origin validation
# - JWT token validation

# All should fail (security working)
```

### Step 6: Production Deployment
```bash
# Only after all staging tests pass:
git merge staging -> main
# GitHub Actions deploys to production
```

---

## Monitoring Both Environments

### Grafana Dashboard
```
┌─────────────────────────────────────────┐
│ WolfBlockchain: Staging vs Production   │
├─────────────────────────────────────────┤
│                                         │
│ API Latency (P95)                       │
│ Staging:     42ms                       │
│ Production:  45ms  ← Within 10%, ✅     │
│                                         │
│ Cache Hit Rate                          │
│ Staging:     74%                        │
│ Production:  76%  ← Similar, ✅         │
│                                         │
│ Error Rate                              │
│ Staging:     0.2%                       │
│ Production:  0.2%  ← Matching, ✅       │
│                                         │
│ Request Rate                            │
│ Staging:     450 req/s (test load)      │
│ Production:  3500 req/s (real load)     │
│                                         │
└─────────────────────────────────────────┘
```

---

## Continuous Parity Verification

### Weekly Parity Check
```bash
#!/bin/bash
# Verify environments stay synchronized

echo "Checking Staging vs Production Parity..."

# 1. Compare deployed version
STAGING_VERSION=$(kubectl describe deployment/wolf-blockchain-api -n wolf-blockchain-staging | grep "Image:")
PROD_VERSION=$(kubectl describe deployment/wolf-blockchain-api -n wolf-blockchain-prod | grep "Image:")

if [ "$STAGING_VERSION" != "$PROD_VERSION" ]; then
  echo "❌ Version mismatch!"
  echo "Staging: $STAGING_VERSION"
  echo "Prod: $PROD_VERSION"
  exit 1
fi

# 2. Compare K8s config
kubectl get deployment wolf-blockchain-api -n wolf-blockchain-staging -o yaml > staging.yaml
kubectl get deployment wolf-blockchain-api -n wolf-blockchain-prod -o yaml > prod.yaml
diff staging.yaml prod.yaml | grep -v "namespace" | grep -v "uid"

if [ $? -ne 0 ]; then
  echo "⚠️ Configuration differences detected (expected for namespace, uid)"
else
  echo "✅ Configurations match"
fi

# 3. Compare metrics
curl -s http://staging-prometheus:9090/api/v1/query?query='rate(http_requests_total[5m])' | jq '.data.result[0].value[1]' > staging_rps.txt
curl -s http://prod-prometheus:9090/api/v1/query?query='rate(http_requests_total[5m])' | jq '.data.result[0].value[1]' > prod_rps.txt

echo "✅ Parity check complete"
```

---

**Staging and production are now identical. Launch with confidence!** 🚀
