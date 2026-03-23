# 🚀 WOLF BLOCKCHAIN - KUBERNETES DEPLOYMENT GUIDE
## Production-Ready Deployment on Kubernetes

---

## 📋 TABLE OF CONTENTS

1. [Prerequisites](#prerequisites)
2. [Quick Start](#quick-start)
3. [Detailed Deployment Steps](#detailed-deployment-steps)
4. [Configuration Management](#configuration-management)
5. [Monitoring & Observability](#monitoring--observability)
6. [Scaling & Performance](#scaling--performance)
7. [Troubleshooting](#troubleshooting)
8. [Backup & Recovery](#backup--recovery)

---

## 📋 PREREQUISITES

### Required Tools
- ✅ `kubectl` (v1.20+) - Kubernetes CLI
- ✅ `helm` (v3+) - Optional, for easier management
- ✅ `docker` - For building images
- ✅ `docker` registry or Docker Hub account

### Kubernetes Cluster Requirements
- ✅ Kubernetes 1.20+
- ✅ Minimum 3 worker nodes
- ✅ Minimum 2GB RAM per node
- ✅ Storage provisioner (local, NFS, cloud provider)
- ✅ Ingress controller (nginx recommended)
- ✅ LoadBalancer support

### Verify Prerequisites
```bash
# Check kubectl
kubectl version --client

# Check cluster access
kubectl cluster-info
kubectl get nodes

# Check storage classes
kubectl get storageclasses
```

---

## 🚀 QUICK START

### 1. Build Docker Image
```bash
# Build image
docker build -t wolfblockchain:latest .
docker build -t wolfblockchain:1.0.0 .

# Tag for registry
docker tag wolfblockchain:latest your-registry/wolfblockchain:latest
docker tag wolfblockchain:latest your-registry/wolfblockchain:1.0.0

# Push to registry
docker push your-registry/wolfblockchain:latest
docker push your-registry/wolfblockchain:1.0.0
```

### 2. Create Namespace
```bash
kubectl apply -f k8s/01-namespace.yaml

# Verify
kubectl get namespace wolf-blockchain
```

### 3. Create Secrets
```bash
# Apply safe template from repo (placeholders only)
kubectl apply -f k8s/03-secret.yaml

# Create/update real production secrets directly in cluster (recommended)
kubectl create secret generic wolf-blockchain-secrets \
  -n wolf-blockchain \
  --from-literal=SA_PASSWORD='YOUR_STRONG_SA_PASSWORD' \
  --from-literal=ConnectionStrings__DefaultConnection='Server=wolf-blockchain-db;Database=WolfBlockchainDb;User=sa;Password=YOUR_STRONG_SA_PASSWORD;Encrypt=true;TrustServerCertificate=true;Connection Timeout=30;MultipleActiveResultSets=true;' \
  --from-literal=Jwt__Secret='YOUR_32_PLUS_CHARACTER_SECRET' \
  --from-literal=Security__BootstrapToken='YOUR_BOOTSTRAP_TOKEN' \
  --from-literal=Security__AdminAllowedIps__0='YOUR_STATIC_ADMIN_IP' \
  --from-literal=Security__AdminAllowedIps__1='YOUR_VPN_OR_BACKUP_ADMIN_IP' \
  --from-literal=Security__AllowedOrigins__0='https://admin.your-domain.tld' \
  --from-literal=Security__AllowedOrigins__1='https://app.your-domain.tld' \
  --from-literal=ExternalServices__ApiKey='YOUR_EXTERNAL_API_KEY' \
  --from-literal=WOLF_TOKEN_SECRET='YOUR_TOKEN_SECRET' \
  --dry-run=client -o yaml | kubectl apply -f -

# Verify
kubectl get secrets -n wolf-blockchain
```

### 4. Create ConfigMaps
```bash
kubectl apply -f k8s/02-configmap.yaml

# Verify
kubectl get configmaps -n wolf-blockchain
```

### 5. Create Storage
```bash
kubectl apply -f k8s/04-pvc.yaml

# Verify
kubectl get pvc -n wolf-blockchain
```

### 6. Deploy Database
```bash
kubectl apply -f k8s/05-statefulset-db.yaml

# Wait for database to be ready
kubectl wait --for=condition=Ready pod -l app=wolf-blockchain,component=database -n wolf-blockchain --timeout=300s

# Verify
kubectl get statefulset -n wolf-blockchain
kubectl get pods -n wolf-blockchain
```

### 7. Create Services
```bash
kubectl apply -f k8s/06-services.yaml

# Verify
kubectl get svc -n wolf-blockchain
```

### 8. Deploy API
```bash
# Update 07-deployment.yaml image if needed
kubectl apply -f k8s/07-deployment.yaml

# Wait for deployment
kubectl wait --for=condition=available --timeout=300s deployment/wolf-blockchain-api -n wolf-blockchain

# Verify
kubectl get deployment -n wolf-blockchain
kubectl get pods -n wolf-blockchain
```

### 9. Configure Auto-Scaling
```bash
kubectl apply -f k8s/08-hpa.yaml

# Verify
kubectl get hpa -n wolf-blockchain
```

### 10. Setup Ingress
```bash
# Update 09-ingress.yaml with your domain
kubectl apply -f k8s/09-ingress.yaml

# Verify
kubectl get ingress -n wolf-blockchain
```

### 11. Setup Monitoring
```bash
kubectl apply -f k8s/10-prometheus-config.yaml
kubectl apply -f k8s/11-prometheus-deployment.yaml

# Verify
kubectl get deployment prometheus -n wolf-blockchain
```

---

## 📊 VERIFY DEPLOYMENT

```bash
# Check all resources
kubectl get all -n wolf-blockchain

# Check pod status
kubectl get pods -n wolf-blockchain -w

# Check pod logs
kubectl logs -n wolf-blockchain deployment/wolf-blockchain-api

# Check services
kubectl get svc -n wolf-blockchain

# Port forward to test locally
kubectl port-forward -n wolf-blockchain svc/wolf-blockchain-api 5000:5000

# Test health endpoint
curl http://localhost:5000/health
```

---

## 🔧 CONFIGURATION MANAGEMENT

### Environment Variables
All configurable via ConfigMap and Secrets:

```bash
# View ConfigMap
kubectl get configmap -n wolf-blockchain wolf-blockchain-config -o yaml

# Edit ConfigMap
kubectl edit configmap -n wolf-blockchain wolf-blockchain-config

# View Secrets
kubectl get secrets -n wolf-blockchain wolf-blockchain-secrets -o yaml

# Update Secrets (recommended: direct cluster update, no real values in git files)
kubectl create secret generic wolf-blockchain-secrets \
  --from-literal=Jwt__Secret=YOUR_NEW_SECRET \
  -n wolf-blockchain --dry-run=client -o yaml | kubectl apply -f -
```

### Database Connection
Set directly in cluster secret:
```
ConnectionStrings__DefaultConnection=Server=wolf-blockchain-db;Database=WolfBlockchainDb;User=sa;Password=YOUR_PASSWORD;...
```

### JWT Secret
Set directly in cluster secret:
```
Jwt__Secret=YOUR_32_PLUS_CHARACTER_SECRET_HERE
```

### Admin IPs
Set directly in cluster secret (or in ConfigMap if intentionally non-sensitive):
```
Security__AdminAllowedIps__0=YOUR_IP_ADDRESS
```

---

## 🔍 MONITORING & OBSERVABILITY

### Check Metrics
```bash
# Port forward to Prometheus
kubectl port-forward -n wolf-blockchain svc/prometheus 9090:9090

# Access Prometheus UI
# http://localhost:9090

# Query example metrics
# up{job="wolf-blockchain-api"}
# container_memory_working_set_bytes{pod=~"wolf-blockchain-api-.*"}
# rate(http_requests_total[5m])
```

### View Logs
```bash
# Stream logs
kubectl logs -n wolf-blockchain -f deployment/wolf-blockchain-api

# View specific pod logs
kubectl logs -n wolf-blockchain POD_NAME

# View previous pod logs (for crashed pods)
kubectl logs -n wolf-blockchain POD_NAME --previous

# View last 100 lines
kubectl logs -n wolf-blockchain deployment/wolf-blockchain-api --tail=100
```

### Check Events
```bash
# Watch cluster events
kubectl get events -n wolf-blockchain -w

# Check pod events
kubectl describe pod -n wolf-blockchain POD_NAME
```

---

## 📈 SCALING & PERFORMANCE

### Manual Scaling
```bash
# Scale to specific number of replicas
kubectl scale deployment wolf-blockchain-api --replicas=5 -n wolf-blockchain

# Verify
kubectl get deployment -n wolf-blockchain
```

### Auto-Scaling Status
```bash
# Check HPA status
kubectl get hpa -n wolf-blockchain -w

# Get HPA details
kubectl describe hpa -n wolf-blockchain wolf-blockchain-api-hpa

# View HPA metrics
kubectl get hpa -n wolf-blockchain --watch
```

### Resource Limits
Edit in `07-deployment.yaml`:
```yaml
resources:
  requests:
    memory: "512Mi"
    cpu: "250m"
  limits:
    memory: "1Gi"
    cpu: "1000m"
```

### Performance Tuning
```bash
# Check pod resource usage
kubectl top pods -n wolf-blockchain

# Check node resource usage
kubectl top nodes
```

---

## 🔄 ROLLING UPDATES & ROLLBACK

### Update Image
```bash
# Set new image
kubectl set image deployment/wolf-blockchain-api \
  api=your-registry/wolfblockchain:1.1.0 \
  -n wolf-blockchain --record

# Watch rollout
kubectl rollout status deployment/wolf-blockchain-api -n wolf-blockchain -w

# View rollout history
kubectl rollout history deployment/wolf-blockchain-api -n wolf-blockchain
```

### Rollback
```bash
# Rollback to previous version
kubectl rollout undo deployment/wolf-blockchain-api -n wolf-blockchain

# Rollback to specific revision
kubectl rollout undo deployment/wolf-blockchain-api --to-revision=2 -n wolf-blockchain
```

---

## 🛠️ TROUBLESHOOTING

### Pod Not Starting
```bash
# Check pod status
kubectl describe pod -n wolf-blockchain POD_NAME

# Check logs
kubectl logs -n wolf-blockchain POD_NAME

# Check events
kubectl get events -n wolf-blockchain --sort-by='.lastTimestamp'
```

### ImagePullBackOff
```bash
# Verify image exists in registry
docker pull your-registry/wolfblockchain:latest

# Update image in deployment
kubectl set image deployment/wolf-blockchain-api \
  api=your-registry/wolfblockchain:latest \
  -n wolf-blockchain
```

### CrashLoopBackOff
```bash
# Check previous logs
kubectl logs -n wolf-blockchain POD_NAME --previous

# Check resource constraints
kubectl describe pod -n wolf-blockchain POD_NAME

# Check liveness/readiness probes
kubectl logs -n wolf-blockchain POD_NAME
```

### Database Connection Issues
```bash
# Check database pod
kubectl get pods -n wolf-blockchain -l component=database

# Check database logs
kubectl logs -n wolf-blockchain wolf-blockchain-db-0

# Test database connectivity
kubectl exec -it -n wolf-blockchain wolf-blockchain-api-POD -- \
  curl http://wolf-blockchain-db:1433
```

Important: updating `wolf-blockchain-secrets` does not automatically rotate SQL `sa` credentials for an already initialized DB volume. If the DB was created with an old password, either update the connection string to that existing password or perform a planned DB credential rotation/reprovision.

### Network Issues
```bash
# Check DNS
kubectl run -it --rm debug --image=busybox --restart=Never -- \
  nslookup wolf-blockchain-db

# Test connectivity
kubectl run -it --rm debug --image=busybox --restart=Never -- \
  wget -O- http://wolf-blockchain-api:5000/health
```

---

## 💾 BACKUP & RECOVERY

### Backup Database
```bash
# Exec into database pod
kubectl exec -it -n wolf-blockchain wolf-blockchain-db-0 -- /bin/bash

# Create backup
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P YOUR_PASSWORD \
  -Q "BACKUP DATABASE WolfBlockchainDb TO DISK='/var/opt/mssql/backup/db.bak'"
```

### Restore Database
```bash
# Exec into database pod
kubectl exec -it -n wolf-blockchain wolf-blockchain-db-0 -- /bin/bash

# Restore from backup
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P YOUR_PASSWORD \
  -Q "RESTORE DATABASE WolfBlockchainDb FROM DISK='/var/opt/mssql/backup/db.bak'"
```

### Backup Persistent Volumes
```bash
# List PVCs
kubectl get pvc -n wolf-blockchain

# Create snapshot (cloud provider specific)
# For Azure: az snapshot create
# For AWS: aws ec2 create-snapshot
# For GCP: gcloud compute snapshots create
```

---

## 🔐 Security Best Practices

✅ Use NetworkPolicies to restrict traffic
✅ Use RBAC for authorization
✅ Use Secrets for sensitive data (not ConfigMaps)
✅ Run containers as non-root
✅ Use read-only root filesystems
✅ Drop unnecessary Linux capabilities
✅ Use resource limits
✅ Enable audit logging
✅ Use TLS for all communication
✅ Regularly scan images for vulnerabilities

---

## 📞 SUPPORT

For issues or questions:
1. Check logs: `kubectl logs -n wolf-blockchain POD_NAME`
2. Describe resource: `kubectl describe pod -n wolf-blockchain POD_NAME`
3. Check events: `kubectl get events -n wolf-blockchain`
4. Review manifests in `k8s/` directory

---

**Happy Deploying!** 🚀
