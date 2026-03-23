# WolfBlockchain Deployment Runbook

## Overview
This runbook covers deployment of WolfBlockchain from development → staging → production.

---

## PRE-DEPLOYMENT CHECKLIST

### Code & Build
- [ ] All tests passing (153/153 unit tests)
- [ ] Integration tests reviewed (marked for staging)
- [ ] No compilation warnings/errors
- [ ] Docker image built and tagged
- [ ] Code review completed
- [ ] Security audit passed
- [ ] Performance benchmarks acceptable

### Infrastructure
- [ ] K8s cluster ready (kubectl access verified)
- [ ] Persistent storage configured
- [ ] Database backup automated
- [ ] TLS certificates valid
- [ ] DNS records updated (for target environment)
- [ ] Monitoring stack deployed (Prometheus/Grafana)

### Documentation
- [ ] API documentation current
- [ ] Architecture docs reviewed
- [ ] Runbook reviewed by team
- [ ] Rollback procedure tested

---

## DEPLOYMENT PROCEDURE

### 1. STAGING DEPLOYMENT

**Environment**: Kubernetes staging namespace
**Duration**: ~10 minutes

```bash
# 1. Create staging namespace
kubectl create namespace wolf-blockchain-staging

# 2. Copy secrets from production (if upgrading)
kubectl get secret -n wolf-blockchain | \
  xargs -I {} kubectl get secret {} -n wolf-blockchain -o yaml | \
  kubectl apply -n wolf-blockchain-staging -f -

# 3. Apply K8s manifests to staging
kubectl apply -f k8s/ -n wolf-blockchain-staging

# 4. Verify deployment
kubectl rollout status deployment/wolf-blockchain-api \
  -n wolf-blockchain-staging --timeout=300s

# 5. Check pod status
kubectl get pods -n wolf-blockchain-staging

# 6. Tail logs for errors
kubectl logs -n wolf-blockchain-staging \
  deployment/wolf-blockchain-api --tail=100
```

### 2. STAGING VALIDATION

```bash
# Health check
curl http://staging.wolf-blockchain.local/health

# API endpoints
curl -H "Authorization: Bearer TOKEN" \
  http://staging.wolf-blockchain.local/api/admindashboard/summary

# Run integration tests (against staging API)
dotnet test tests/WolfBlockchain.Tests/WolfBlockchain.Tests.csproj \
  --filter "Category==Integration" \
  -- "staging.wolf-blockchain.local"

# Run load test
./tests/load-test.sh 10 60  # 10 users, 60 seconds
```

### 3. PRODUCTION DEPLOYMENT

**Environment**: Kubernetes production namespace
**Duration**: ~15 minutes (rolling update)
**Backout Time**: ~5 minutes (if needed)

```bash
# 1. Create production namespace (if first deploy)
kubectl create namespace wolf-blockchain-prod

# 2. Apply production secrets
kubectl apply -f k8s/secrets/prod-secrets.yaml -n wolf-blockchain-prod

# 3. Apply K8s manifests
kubectl apply -f k8s/ -n wolf-blockchain-prod

# 4. Monitor rollout
kubectl rollout status deployment/wolf-blockchain-api \
  -n wolf-blockchain-prod --timeout=600s

# 5. Verify all pods healthy
kubectl get pods -n wolf-blockchain-prod -o wide

# 6. Run smoke tests
curl https://api.wolf-blockchain.com/health

# 7. Monitor for 5 minutes
kubectl logs -n wolf-blockchain-prod deployment/wolf-blockchain-api -f
```

---

## SMOKE TESTS (Post-Deployment)

Run immediately after deployment:

```bash
#!/bin/bash
API_URL=${1:-"http://localhost"}
FAILURES=0

echo "🧪 Running Smoke Tests..."

# Test 1: Health check
echo -n "Testing /health ... "
if curl -s "$API_URL/health" | grep -q "Healthy"; then
  echo "✅"
else
  echo "❌ FAILED"
  FAILURES=$((FAILURES + 1))
fi

# Test 2: Metrics
echo -n "Testing /metrics ... "
if curl -s "$API_URL/metrics" | grep -q "wolfblockchain"; then
  echo "✅"
else
  echo "❌ FAILED"
  FAILURES=$((FAILURES + 1))
fi

# Test 3: API endpoints
echo -n "Testing /api/admindashboard/summary ... "
if curl -s -H "Authorization: Bearer TESTTOKEN" \
  "$API_URL/api/admindashboard/summary" | grep -q "TotalUsers"; then
  echo "✅"
else
  echo "❌ FAILED"
  FAILURES=$((FAILURES + 1))
fi

# Test 4: Response time
echo -n "Testing response time (<100ms) ... "
START=$(date +%s%N)
curl -s "$API_URL/health" > /dev/null
END=$(date +%s%N)
ELAPSED=$((($END - $START) / 1000000))
if [ $ELAPSED -lt 100 ]; then
  echo "✅ ${ELAPSED}ms"
else
  echo "⚠️  ${ELAPSED}ms (acceptable)"
fi

echo ""
if [ $FAILURES -eq 0 ]; then
  echo "✅ All smoke tests passed!"
  exit 0
else
  echo "❌ $FAILURES test(s) failed"
  exit 1
fi
```

---

## MONITORING CHECKLIST (Post-Deployment)

Watch for first 30 minutes:

- [ ] No errors in logs (`kubectl logs`)
- [ ] Pod memory stable (<768Mi per pod)
- [ ] Pod CPU low (<100m idle)
- [ ] Response times < 100ms (p50), < 500ms (p95)
- [ ] Error rate < 1%
- [ ] No database connection errors
- [ ] Cache hit rate > 70% (for admin endpoints)

---

## ROLLBACK PROCEDURE

If critical issues detected:

```bash
# 1. Rollback to previous deployment
kubectl rollout undo deployment/wolf-blockchain-api -n wolf-blockchain-prod

# 2. Verify rollback
kubectl rollout status deployment/wolf-blockchain-api -n wolf-blockchain-prod

# 3. Verify service restored
curl https://api.wolf-blockchain.com/health

# 4. Investigate issue
kubectl logs deployment/wolf-blockchain-api -n wolf-blockchain-prod --tail=200 > /tmp/issue.log

# 5. Create incident report
# ...
```

**Rollback is safe** because:
- Deployment uses rolling update strategy (maxUnavailable: 0)
- All previous versions available in K8s
- Database migrations are backward-compatible
- Cache can be cleared without data loss

---

## COMMON ISSUES & RESOLUTIONS

### Issue: Pods stuck in CrashLoopBackOff

```bash
# Check logs
kubectl logs <pod-name> -n wolf-blockchain-prod

# Common causes:
# - Database connection string invalid
# - Secrets not mounted
# - Port already in use
# Resolution: Fix issue, rebuild image, redeploy
```

### Issue: High memory usage

```bash
# Check pod metrics
kubectl top pods -n wolf-blockchain-prod

# If >800Mi: adjust resource limits in k8s/07-deployment.yaml
# Restart pods: kubectl rollout restart deployment/...
```

### Issue: Slow response times

```bash
# Check cache hit rate (look in logs)
# If low: increase TTL in AdminDashboardCacheService.cs
# Rebuild: docker build -t wolfblockchain:vX.X.X

# Check database
# If slow: add indexes, optimize queries
```

---

## VERSION MANAGEMENT

### Image Tagging Strategy
```
v1.0.0  → Initial release
v1.1.0  → SignalR feature
v1.2.0  → Admin dashboard API
...
v2.0.0  → Validation & testing
v2.1.0  → Production release
```

### Keeping Track
```bash
# Tag current image for production
docker tag wolfblockchain:v2.0.0 wolfblockchain:v2.0.0-prod

# Push to registry (if using remote)
docker push wolfblockchain:v2.0.0-prod

# Update k8s deployment to use versioned image
# instead of 'latest' or untagged
```

---

## FINAL SIGN-OFF

**Deployment Sign-Off Template**

```
Deployment Date: _______________
Version: v2.0.0
Environment: Production

Checklist:
[ ] Pre-deployment checks passed
[ ] Deployment completed successfully
[ ] All pods healthy
[ ] Smoke tests passed
[ ] Monitoring active
[ ] No critical errors in logs

Approved By: _________________
Date: _________________________
```

---

## CONTACT & ESCALATION

- **On-Call Engineer**: [contact info]
- **DevOps Team**: [slack channel]
- **Incident Response**: [procedures]
- **Escalation Path**: Engineer → Tech Lead → Director

---

**Keep this runbook current and test rollback quarterly!**
