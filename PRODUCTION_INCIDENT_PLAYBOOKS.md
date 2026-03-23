# 🚨 PRODUCTION INCIDENT RESPONSE PLAYBOOKS

---

## PLAYBOOK 1: API IS DOWN (COMPLETE OUTAGE)

**Severity**: CRITICAL 🔴
**Response Time**: < 5 minutes

### Detection
- Health endpoint returns anything other than 200
- Error rate spikes to 100%
- Prometheus alerts: "ApiDown"
- Slack alert received

### Step 1: IMMEDIATE RESPONSE (< 1 minute)

```bash
# 1. Acknowledge alert
# Slack: "Acknowledged, investigating..."

# 2. Check pod status
kubectl get pods -n wolf-blockchain-prod
# Look for: Any pods not "Running" or "Ready"

# 3. Describe failing pods
kubectl describe pod [pod-name] -n wolf-blockchain-prod
# Look for: OOMKilled, CrashLoopBackOff, Pending

# 4. Check recent logs
kubectl logs deployment/wolf-blockchain-api -n wolf-blockchain-prod --tail=50
# Look for: Error messages, exceptions
```

### Step 2: TRIAGE (1-5 minutes)

**Is it a Pod Issue?**
```bash
kubectl get events -n wolf-blockchain-prod --sort-by='.lastTimestamp'
# If recent failures → Pod issue
# If no events → Kubernetes issue
```

**Is it a Database Issue?**
```bash
# Try to connect to database manually
sqlcmd -S [prod-db-server] -U sa -P $password -Q "SELECT @@VERSION"
# If timeout → Database issue
# If connection OK → App issue
```

**Is it a Networking Issue?**
```bash
# Check ingress
kubectl describe ingress wolf-blockchain-api -n wolf-blockchain-prod
# Look for: backend endpoints, status

# Check DNS
nslookup api.wolf-blockchain.com
# Look for: DNS resolution working
```

### Step 3: RESOLUTION

**If Pod Issue:**
```bash
# Restart the deployment
kubectl rollout restart deployment/wolf-blockchain-api -n wolf-blockchain-prod

# Wait for rollout
kubectl rollout status deployment/wolf-blockchain-api -n wolf-blockchain-prod

# Verify health
curl https://api.wolf-blockchain.com/health
```

**If Database Issue:**
```bash
# Contact database team immediately
# Escalate to database on-call: [phone]

# Temporary: Failover to replica (if available)
# Check K8s secret for database endpoint
kubectl get secret wolf-blockchain-secrets -n wolf-blockchain-prod -o yaml
```

**If Networking Issue:**
```bash
# Check ingress configuration
kubectl get ingress -n wolf-blockchain-prod

# Restart ingress controller
kubectl rollout restart deployment/ingress-nginx -n ingress-nginx

# Verify connectivity
curl -v https://api.wolf-blockchain.com/health
```

### Step 4: IF STILL DOWN → ROLLBACK

```bash
# Immediate rollback
kubectl rollout undo deployment/wolf-blockchain-api -n wolf-blockchain-prod

# Wait for completion
kubectl rollout status deployment/wolf-blockchain-api -n wolf-blockchain-prod --timeout=5m

# Verify
curl https://api.wolf-blockchain.com/health
```

### Step 5: POST-INCIDENT

```bash
# Document incident
echo "INCIDENT: API Down at $(date)" >> incidents.log
echo "Cause: [root cause]" >> incidents.log
echo "Resolution: [how fixed]" >> incidents.log

# Slack: "@channel API restored. Post-mortem scheduled for tomorrow 10 AM"

# Schedule post-mortem meeting within 24 hours
```

---

## PLAYBOOK 2: HIGH ERROR RATE (> 5%)

**Severity**: HIGH 🟠
**Response Time**: < 10 minutes

### Detection
- Prometheus alert: "HighErrorRate"
- Error rate > 5% for > 2 minutes
- Slack alert with error rate percentage

### Immediate Response

```bash
# 1. Check error logs
kubectl logs deployment/wolf-blockchain-api -n wolf-blockchain-prod -f --tail=100 | grep -i error

# 2. Identify error pattern
# Look for repeated error messages:
# - "Database connection timeout"
# - "JWT validation failed"
# - "Rate limit exceeded"
# - "Memory exceeded"

# 3. Check system metrics
kubectl top pods -n wolf-blockchain-prod
kubectl top nodes
# Look for high memory/CPU
```

### Resolution by Error Type

**Database Connection Errors:**
```bash
# Check DB connectivity
sqlcmd -S [db-server] -U sa -Q "SELECT COUNT(*) FROM Users"

# If times out → Database issue
# Increase connection pool size or reduce traffic

# Reduce replica count temporarily
kubectl scale deployment wolf-blockchain-api --replicas=1 -n wolf-blockchain-prod
sleep 60
# Monitor error rate
# If improving → scale back up slowly
kubectl scale deployment wolf-blockchain-api --replicas=3 -n wolf-blockchain-prod
```

**JWT Validation Errors:**
```bash
# Check secret is correct
kubectl describe secret wolf-blockchain-secrets -n wolf-blockchain-prod

# Check JWT configuration in logs
kubectl logs deployment/wolf-blockchain-api -n wolf-blockchain-prod | grep -i jwt

# If secret was recently rotated:
# Clear application cache
kubectl exec -it [pod-name] -n wolf-blockchain-prod -- curl -X DELETE http://localhost/cache/jwt

# Restart pods
kubectl rollout restart deployment/wolf-blockchain-api -n wolf-blockchain-prod
```

**Memory Pressure:**
```bash
# Check memory usage
kubectl top pods -n wolf-blockchain-prod --containers

# If any pod > 750Mi → OOM risk
# Scale to more pods (distribute load)
kubectl scale deployment wolf-blockchain-api --replicas=5 -n wolf-blockchain-prod

# Or increase pod memory limits
kubectl set resources deployment wolf-blockchain-api -n wolf-blockchain-prod --limits=memory=1Gi
```

**Rate Limiting Exceeded:**
```bash
# This is expected if traffic spike
# Scale up more pods
kubectl scale deployment wolf-blockchain-api --replicas=10 -n wolf-blockchain-prod

# Monitor error rate decreases
# Adjust rate limiting thresholds if needed
# (requires restart with new config)
```

---

## PLAYBOOK 3: SLOW PERFORMANCE (P95 > 1 second)

**Severity**: MEDIUM 🟡
**Response Time**: < 15 minutes

### Detection
- Prometheus alert: "HighLatency"
- P95 latency > 1 second
- Users report slow responses

### Investigation

```bash
# 1. Check which endpoint is slow
curl -w '@curl-format.txt' https://api.wolf-blockchain.com/api/endpoint
# Look for high response time

# 2. Check database performance
sqlcmd -S [db-server] -U sa -Q "
  SELECT TOP 10 
    query_stats.total_elapsed_time,
    database_name,
    object_name
  FROM sys.dm_exec_procedure_stats
  ORDER BY total_elapsed_time DESC
"

# 3. Check cache hit rate
curl -s https://api.wolf-blockchain.com/metrics | grep cache_hits
# If hit rate < 60% → Cache misconfigured
```

### Resolution

**Database Query is Slow:**
```bash
# Add index if missing
sqlcmd -S [db-server] -U sa -Q "
  CREATE INDEX idx_users_username ON Users(username)
"

# Or check for missing STATISTICS
sqlcmd -S [db-server] -U sa -Q "
  UPDATE STATISTICS [WolfBlockchain]
"
```

**Cache Hit Rate Low:**
```bash
# Check cache TTL configuration
# In appsettings.json: "Cache": { "DefaultTtlMinutes": 5 }

# If should be higher, update:
kubectl set env deployment/wolf-blockchain-api \
  CACHE_DEFAULT_TTL_MINUTES=15 \
  -n wolf-blockchain-prod

# Restart pods
kubectl rollout restart deployment/wolf-blockchain-api -n wolf-blockchain-prod
```

**Database Connection Pool Exhausted:**
```bash
# Check current connections
sqlcmd -S [db-server] -U sa -Q "SELECT @@CONNECTIONS"

# Scale down pods to reduce load
kubectl scale deployment wolf-blockchain-api --replicas=2 -n wolf-blockchain-prod

# Then scale back up slowly while monitoring
kubectl scale deployment wolf-blockchain-api --replicas=3 -n wolf-blockchain-prod
sleep 60
kubectl scale deployment wolf-blockchain-api --replicas=4 -n wolf-blockchain-prod
```

---

## PLAYBOOK 4: DATABASE OFFLINE

**Severity**: CRITICAL 🔴
**Response Time**: < 5 minutes

### Detection
- Connection timeout errors in logs
- Error rate spike to 100%
- Health check failing (database connectivity test)

### Immediate Response

```bash
# 1. Verify database is down
sqlcmd -S [db-server] -U sa -Q "SELECT @@VERSION"
# If timeout → Database is offline

# 2. Check database server status
# (Contact data center / AWS / Azure)
# Is server running? Is network accessible?

# 3. Scale down pods to prevent cascading failures
kubectl scale deployment/wolf-blockchain-api --replicas=0 -n wolf-blockchain-prod

# 4. Notify team
Slack: "DATABASE OFFLINE - Scaled down to 0 pods. Waiting for database recovery."
```

### Escalation

```bash
# Contact database team immediately
# DBA on-call: [phone number]

# Provide information:
# - Database server: [hostname]
# - Last successful connection: [time]
# - Error messages: [from logs]
```

### Recovery

```bash
# Once database is back online:

# 1. Verify connectivity
sqlcmd -S [db-server] -U sa -Q "SELECT COUNT(*) FROM Users"

# 2. Scale pods back up
kubectl scale deployment/wolf-blockchain-api --replicas=3 -n wolf-blockchain-prod

# 3. Monitor
kubectl logs deployment/wolf-blockchain-api -n wolf-blockchain-prod -f

# 4. Health check
curl https://api.wolf-blockchain.com/health

# 5. Confirm stable
# Wait 5 minutes, check error rate < 0.5%
```

---

## PLAYBOOK 5: OUT OF MEMORY (OOMKilled)

**Severity**: HIGH 🟠
**Response Time**: < 10 minutes

### Detection
- Pod crashes
- Status: "OOMKilled"
- Memory spikes before restart
- Prometheus alert: "HighMemoryUsage"

### Investigation

```bash
# Check which pod is OOM
kubectl get pods -n wolf-blockchain-prod
# Look for: Restart count > 0

# Check pod memory
kubectl describe pod [pod-name] -n wolf-blockchain-prod
# Look for: "Reason: OOMKilled"

# Check logs
kubectl logs [pod-name] -n wolf-blockchain-prod --previous
# Look for: Memory increase over time
```

### Resolution

**Option 1: Increase Pod Memory Limit**
```bash
kubectl set resources deployment/wolf-blockchain-api \
  -n wolf-blockchain-prod \
  --limits=memory=1Gi \
  --requests=memory=512Mi

kubectl rollout restart deployment/wolf-blockchain-api -n wolf-blockchain-prod
```

**Option 2: Scale to More Pods (Distribute Load)**
```bash
# Add more replicas to distribute memory load
kubectl scale deployment/wolf-blockchain-api --replicas=5 -n wolf-blockchain-prod

# Monitor if OOMKilled stops happening
kubectl get pods -n wolf-blockchain-prod -w
```

**Option 3: Find Memory Leak**
```bash
# Enable detailed memory profiling
kubectl set env deployment/wolf-blockchain-api \
  ENABLE_MEMORY_PROFILING=true \
  -n wolf-blockchain-prod

# Collect memory dump
kubectl exec [pod-name] -n wolf-blockchain-prod -- /app/memory-dump.sh

# Analyze with dotnet-dump or Visual Studio
# Look for: Unreleased objects, circular references
```

---

## PLAYBOOK 6: DISK SPACE CRITICAL

**Severity**: MEDIUM 🟡
**Response Time**: < 30 minutes

### Detection
- Prometheus alert: "DiskSpaceWarning" or "DiskSpaceCritical"
- Disk usage > 85%
- New pods can't be scheduled

### Investigation

```bash
# Check disk usage per node
kubectl top nodes
# Look for: High disk usage

# Check PVC usage
kubectl get pvc -n wolf-blockchain-prod

# Check what's using space
kubectl exec -it [pod-name] -n wolf-blockchain-prod -- df -h
kubectl exec -it [pod-name] -n wolf-blockchain-prod -- du -sh /app/logs/*
```

### Resolution

**Clean Up Old Logs:**
```bash
# Delete old logs (older than 30 days)
kubectl exec [pod-name] -n wolf-blockchain-prod -- find /app/logs -mtime +30 -delete

# Restart to reload logs
kubectl rollout restart deployment/wolf-blockchain-api -n wolf-blockchain-prod
```

**Expand Persistent Volume:**
```bash
# Increase PVC size
kubectl patch pvc wolf-blockchain-pvc -n wolf-blockchain-prod -p \
  '{"spec":{"resources":{"requests":{"storage":"100Gi"}}}}'

# Pods automatically get new space
```

**Archive Old Data:**
```bash
# Backup old data to S3
aws s3 sync /app/data s3://wolf-blockchain-backups/$(date +%Y-%m-%d)/

# Delete local old data
find /app/data -mtime +90 -delete
```

---

## PLAYBOOK 7: SECURITY INCIDENT

**Severity**: CRITICAL 🔴
**Response Time**: < 5 minutes

### Detection
- Unauthorized access attempt
- SQL injection detected
- XSS payload detected
- Brute force attack
- Data exfiltration suspected

### Immediate Response

```bash
# 1. ISOLATE (if necessary)
# If attack in progress, scale down to prevent damage
kubectl scale deployment/wolf-blockchain-api --replicas=0 -n wolf-blockchain-prod

# 2. COLLECT EVIDENCE
# Don't delete anything - might need for forensics
kubectl logs deployment/wolf-blockchain-api -n wolf-blockchain-prod > incident-logs.txt
kubectl describe all -n wolf-blockchain-prod > incident-state.txt

# 3. NOTIFY SECURITY TEAM
Slack: "@security-lead SECURITY INCIDENT - Scaling down pending investigation"
Email: security@wolf-blockchain.com

# 4. CONTACT EXECUTIVES
# CTO / CISO should be informed immediately
```

### Investigation

```bash
# Check security logs
grep -i "unauthorized\|attack\|injection\|xss" logs/security-audit-*.txt

# Review access logs
grep -E "(admin|/secret|password)" logs/security-audit-*.txt

# Check for suspicious IPs
awk '{print $1}' logs/security-audit-*.txt | sort | uniq -c | sort -rn
```

### Remediation

```bash
# 1. Patch if code vulnerability
# Fix code, test, deploy new image

# 2. Rotate credentials
# New JWT secret
# New database password
# New API keys

# 3. Update firewall rules
# Block attacking IP addresses

# 4. Restart clean
kubectl rollout restart deployment/wolf-blockchain-api -n wolf-blockchain-prod

# 5. Monitor
# Watch logs for repeated attacks
```

---

## ESCALATION MATRIX

| Severity | Response Time | Who To Contact | Escalate If |
|----------|--------------|---|---|
| 🟢 Low | 24 hours | Duty Engineer | Not resolved in 4 hours |
| 🟡 Medium | 2 hours | Team Lead | Not resolved in 1 hour |
| 🟠 High | 30 minutes | On-Call Manager | Not resolved in 15 minutes |
| 🔴 Critical | 5 minutes | CTO / VP Eng | Not resolved in 10 minutes |

---

## CONTACT ESCALATION

```
Level 1: Duty Engineer
├─ On-call rotation: [Slack channel]
└─ Phone: [rotating number]

Level 2: Team Lead
├─ DevOps Lead: [phone]
└─ Engineering Manager: [phone]

Level 3: Management
├─ CTO: [phone]
└─ VP Engineering: [phone]

Level 4: Executive
├─ CEO: [phone]
└─ COO: [phone]
```

---

## POST-INCIDENT PROCESS

1. **Immediate**: Resolve the issue, stabilize system
2. **1 hour after**: Brief leadership on what happened
3. **24 hours after**: Schedule post-mortem meeting
4. **Post-mortem meeting**: 
   - What happened?
   - Why did it happen?
   - What did we do right?
   - What should we improve?
   - Action items to prevent recurrence?
5. **Action items**: Implement fixes, test, deploy

---

**All playbooks ready for production incidents** 🚨
