# Backup & Disaster Recovery Strategy

## RTO & RPO Targets

| Scenario | RTO | RPO | Strategy |
|----------|-----|-----|----------|
| **Single Pod Failure** | <1 minute | 0 (stateless) | K8s auto-restart |
| **Multi-Pod Failure** | <5 minutes | 0 (stateless) | HPA scale + rolling restart |
| **Database Corruption** | <1 hour | <24 hours | Daily backup + restore |
| **Entire Cluster Failure** | <4 hours | <24 hours | Multi-region or manual failover |
| **Data Loss** | <24 hours | <7 days | Weekly backup retention |

---

## Backup Strategy

### Database Backups

**Daily Automated Backup**
```bash
# Backup schedule: Daily at 2 AM UTC
0 2 * * * /scripts/backup-database.sh

# Retention: 30 daily backups + 4 weekly + 12 monthly
```

**Backup Script** (`scripts/backup-database.sh`)
```bash
#!/bin/bash

BACKUP_DIR="/backups/database"
DB_HOST="wolf-blockchain-db"
DB_NAME="WolfBlockchain"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
BACKUP_FILE="$BACKUP_DIR/backup-$TIMESTAMP.sql"

# Create backup
sqlcmd -S $DB_HOST -U sa -P $DB_PASSWORD -Q "BACKUP DATABASE $DB_NAME TO DISK='$BACKUP_FILE'"

# Compress
gzip "$BACKUP_FILE"

# Verify
if [ -f "$BACKUP_FILE.gz" ]; then
    echo "✅ Backup successful: $BACKUP_FILE.gz"
    # Upload to cloud storage (S3, Azure Blob, etc)
    aws s3 cp "$BACKUP_FILE.gz" "s3://wolf-blockchain-backups/"
else
    echo "❌ Backup failed"
    # Send alert to Slack/email
    exit 1
fi

# Clean old backups (keep 30 days)
find $BACKUP_DIR -name "*.sql.gz" -mtime +30 -delete
```

### Kubernetes State Backup

**Backup Manifests**
```bash
# Backup all K8s resources
kubectl get all -n wolf-blockchain-prod -o yaml > k8s-backup-$(date +%Y%m%d).yaml

# Backup secrets (WARNING: base64 encoded!)
kubectl get secrets -n wolf-blockchain-prod -o yaml > secrets-backup-$(date +%Y%m%d).yaml
# Store encrypted: gpg --symmetric secrets-backup-*.yaml
```

### Application State Backup

**What to Backup**
```
Database                  → SQL backups (daily)
Configuration             → K8s ConfigMaps (version controlled)
Secrets                   → K8s Secrets (encrypted, separate storage)
Code & Artifacts          → Git repository + Docker registry
Logs                      → ELK stack or cloud logging
Monitoring Configs        → Prometheus/Grafana (version controlled)
```

---

## Restore Procedures

### Single Database Restore

```bash
# Stop application
kubectl scale deployment/wolf-blockchain-api --replicas=0 -n wolf-blockchain-prod

# Restore database
sqlcmd -S $DB_HOST -U sa -P $DB_PASSWORD \
  -Q "RESTORE DATABASE WolfBlockchain FROM DISK='path/to/backup.sql'"

# Verify restore
sqlcmd -S $DB_HOST -U sa -P $DB_PASSWORD -Q "SELECT COUNT(*) FROM Users"

# Restart application
kubectl scale deployment/wolf-blockchain-api --replicas=3 -n wolf-blockchain-prod
```

### Rollback to Previous Docker Image

```bash
# View deployment history
kubectl rollout history deployment/wolf-blockchain-api -n wolf-blockchain-prod

# Rollback to previous version
kubectl rollout undo deployment/wolf-blockchain-api -n wolf-blockchain-prod

# Or rollback to specific revision
kubectl rollout undo deployment/wolf-blockchain-api -n wolf-blockchain-prod --to-revision=5

# Verify
kubectl rollout status deployment/wolf-blockchain-api -n wolf-blockchain-prod
```

### Complete Cluster Restore

```bash
# Restore from manifests
kubectl apply -f k8s-backup-20260322.yaml -n wolf-blockchain-prod

# Restore secrets (use encrypted backup)
gpg --decrypt secrets-backup-20260322.yaml.gpg | kubectl apply -f -

# Wait for pods
kubectl wait --for=condition=Ready pod -l app=wolf-blockchain-api -n wolf-blockchain-prod --timeout=300s

# Verify
kubectl get all -n wolf-blockchain-prod
```

---

## Rollback Scenarios

### Scenario 1: Bad Deployment (Detected Immediately)

```bash
# 1. Alert: Deployment health check fails
# 2. Automatic: K8s marks new pods as unhealthy
# 3. Automatic: Old pods keep serving traffic
# 4. Manual: Undo deployment
kubectl rollout undo deployment/wolf-blockchain-api -n wolf-blockchain-prod

# 5. Verify: Check logs and metrics
kubectl logs -f deployment/wolf-blockchain-api -n wolf-blockchain-prod
```

### Scenario 2: Database Corruption (Detected via Alert)

```bash
# 1. Alert: Database query error rate spike
# 2. Immediate: Scale down pods to prevent more corruption
kubectl scale deployment/wolf-blockchain-api --replicas=1 -n wolf-blockchain-prod

# 3. Restore latest good backup
# ... (restore procedure above)

# 4. Run consistency check
sqlcmd -S $DB_HOST -U sa -P $DB_PASSWORD -Q "DBCC CHECKDB"

# 5. Scale back up
kubectl scale deployment/wolf-blockchain-api --replicas=3 -n wolf-blockchain-prod
```

### Scenario 3: Secrets Compromised (Immediate Action)

```bash
# 1. Rotate compromised secret
TIMESTAMP=$(date +%s)
NEW_SECRET="new-secret-$TIMESTAMP"

# 2. Update K8s secret
kubectl patch secret wolf-blockchain-secrets \
  -n wolf-blockchain-prod \
  -p "{\"stringData\":{\"Jwt__Secret\":\"$NEW_SECRET\"}}"

# 3. Force pod restart (pull new secret)
kubectl rollout restart deployment/wolf-blockchain-api -n wolf-blockchain-prod

# 4. Verify
kubectl logs -f deployment/wolf-blockchain-api -n wolf-blockchain-prod
```

---

## Testing Disaster Recovery

### Monthly DR Drill

```bash
# 1. Restore database to test environment
# Simulate restore from production backup to staging DB

# 2. Run smoke tests
bash scripts/smoke-tests.sh https://staging-dr.wolf-blockchain.local

# 3. Verify data consistency
SELECT COUNT(*) FROM Users;
SELECT COUNT(*) FROM Tokens;
SELECT COUNT(*) FROM SmartContracts;

# 4. Document results
# - Time to restore
# - Data integrity
# - Service availability
# - Any issues found
```

### Quarterly Failover Drill

```bash
# 1. Simulate regional failover
# (if using multi-region setup)

# 2. Redirect traffic to failover region

# 3. Verify all services operational
kubectl get all -n wolf-blockchain-prod

# 4. Test critical flows
- User login
- Token creation
- Smart contract deployment
- AI job creation

# 5. Switch back to primary

# 6. Document lessons learned
```

---

## Monitoring Backup Health

### Backup Status Checks

```prometheus
# Prometheus queries

# Last backup age
(time() - timestamp(backup_timestamp_seconds)) / 3600  # Hours since last backup

# Backup success rate
rate(backup_success_total[24h]) / rate(backup_attempts_total[24h])

# Restore time SLA
restore_duration_seconds < 3600  # Target: < 1 hour
```

### Alerts

```yaml
- alert: BackupMissing
  expr: (time() - max(backup_timestamp_seconds)) > 86400
  annotations:
    summary: "No backup in last 24 hours"
    action: "Check backup service, investigate logs"

- alert: BackupFailed
  expr: backup_success_total == 0
  for: 1h
  annotations:
    summary: "Backup failures detected"
    action: "Check backup script, database connectivity"

- alert: RestoreTimeExceedsSLA
  expr: restore_duration_seconds > 3600
  annotations:
    summary: "Restore took > 1 hour"
    action: "Optimize restore procedure, increase resources"
```

---

## Backup Checklist

- [ ] Daily automated database backups enabled
- [ ] Backups stored in separate location (not on cluster)
- [ ] Backup encryption configured
- [ ] Retention policy defined (30 days minimum)
- [ ] Restore procedure tested monthly
- [ ] K8s manifests version controlled (Git)
- [ ] Secrets backed up separately (encrypted)
- [ ] Monitoring alerts for backup failures
- [ ] Team trained on restore procedures
- [ ] Incident response plan documented

---

## Recovery Time Estimates

| Failure Type | Detection | Mitigation | Full Recovery |
|---------|-----------|-----------|-------------|
| Pod crash | <30 sec | <1 min | <2 min |
| Database connection lost | <5 sec | <30 sec | <2 min |
| Database corruption | <5 min | <30 min | <1 hour |
| Regional outage | <1 min | <5 min | <4 hours |
| Complete data loss | Unknown | <1 hour | <24 hours |

---

**Backup and disaster recovery ensures business continuity.**
