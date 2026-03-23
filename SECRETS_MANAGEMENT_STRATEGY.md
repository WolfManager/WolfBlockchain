# Secrets Management Strategy

## Overview
This document outlines how secrets are managed across development, staging, and production environments.

## Secrets Categories

### 1. Database Credentials
- Connection strings
- Database passwords
- Replication credentials

### 2. Authentication & Authorization
- JWT secret key
- Refresh token secret
- API keys for external services
- OAuth credentials

### 3. Encryption Keys
- Data encryption keys
- Message signing keys
- SSL/TLS certificates

### 4. Third-Party Services
- Email service API keys
- Payment provider keys
- Analytics service tokens
- Cloud storage credentials

### 5. Configuration
- Feature flags
- Service endpoints
- Timeout values
- Rate limit thresholds

---

## Storage Locations by Environment

### Development (Local Machine)
```
Location: User secrets (encrypted by OS)
Command: dotnet user-secrets set "Jwt:Secret" "dev-secret-value"
Stored: %APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json
Access: Only current user
Never: Check into git
```

### Staging Environment
```
Location: Kubernetes Secrets
Type: Opaque (base64 encoded)
Created: kubectl create secret generic wolf-blockchain-secrets -n wolf-blockchain-staging ...
Accessed: Via volume mount in pods
Rotated: Manual + automated scripts
```

### Production Environment
```
Location: Kubernetes Secrets + Vault (future)
Type: Encrypted at rest
Created: kubectl create secret generic wolf-blockchain-secrets -n wolf-blockchain-prod ...
Accessed: Via volume mount + secret provider class (if using CSI)
Rotated: Every 90 days (automated)
Audit: All access logged
```

---

## Implementation

### 1. Local Development (appsettings.Development.json + User Secrets)

**Never commit secrets to git.**

#### appSettings.Development.json (WITHOUT secrets)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=WolfBlockchain;Integrated Security=true;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "Jwt": {
    "Issuer": "http://localhost:5000",
    "Audience": "wolf-blockchain-local"
    // Secret goes in user-secrets, NOT here
  },
  "Security": {
    "SingleAdminMode": true,
    "AllowedOrigins": ["http://localhost:3000", "http://localhost:5000"]
  }
}
```

#### Set User Secrets
```bash
# One-time setup
cd src/WolfBlockchain.API
dotnet user-secrets init

# Set secrets
dotnet user-secrets set "Jwt:Secret" "YOUR_STRONG_SECRET_HERE_MIN_32_CHARS"
dotnet user-secrets set "Security:AdminPassword" "ADMIN_PASSWORD_HERE"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_DEV_CONNECTION_STRING"

# List all secrets
dotnet user-secrets list

# Clear secrets (fresh start)
dotnet user-secrets clear
```

#### .gitignore (ensure secrets not committed)
```
# User secrets
appsettings.*.json
secrets.json
secrets.Development.json

# Environment files
.env
.env.local
.env.*.local

# Database files
*.mdf
*.ldf
*.db

# Logs
logs/
```

---

### 2. Staging Environment (Kubernetes Secrets)

#### Create K8s Secret
```bash
# Create secret from files
kubectl create secret generic wolf-blockchain-secrets \
  --from-literal=Jwt__Secret='staging-jwt-secret-at-least-32-chars' \
  --from-literal=Security__AdminPassword='staging-admin-password' \
  --from-literal=ConnectionStrings__DefaultConnection='Server=wolf-blockchain-db;Database=WolfBlockchain;User=sa;Password=YourStagingPassword;' \
  -n wolf-blockchain-staging

# Or from encrypted file
cat staging-secrets.yaml | kubectl apply -f -
```

#### K8s Secret YAML (staging-secrets.yaml)
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: wolf-blockchain-secrets
  namespace: wolf-blockchain-staging
type: Opaque
stringData:  # Use stringData, not data (no need to base64 encode manually)
  Jwt__Secret: "staging-secret-key-at-least-32-characters-long"
  Security__AdminPassword: "staging-admin-password-change-me"
  ConnectionStrings__DefaultConnection: "Server=wolf-blockchain-db;Database=WolfBlockchain;User=sa;Password=StagingDbPassword;"
  # Add more as needed
```

#### Mount in Deployment
```yaml
# In k8s/07-deployment.yaml
spec:
  containers:
  - name: api
    image: wolfblockchain:staging
    env:
    # From K8s Secret
    - name: Jwt__Secret
      valueFrom:
        secretKeyRef:
          name: wolf-blockchain-secrets
          key: Jwt__Secret
    - name: Security__AdminPassword
      valueFrom:
        secretKeyRef:
          name: wolf-blockchain-secrets
          key: Security__AdminPassword
    - name: ConnectionStrings__DefaultConnection
      valueFrom:
        secretKeyRef:
          name: wolf-blockchain-secrets
          key: ConnectionStrings__DefaultConnection
```

---

### 3. Production Environment (Kubernetes Secrets + Vault)

#### Create Encrypted K8s Secret
```bash
# Create secret with encryption
kubectl create secret generic wolf-blockchain-secrets \
  --from-literal=Jwt__Secret='production-jwt-secret-at-least-32-chars-CHANGE_ME' \
  --from-literal=Security__AdminPassword='production-admin-password-CHANGE_ME' \
  --from-literal=ConnectionStrings__DefaultConnection='Server=prod-db-server;Database=WolfBlockchain;User=sa;Password=ProdDbPasswordHere;Encrypt=true;TrustServerCertificate=false;' \
  -n wolf-blockchain-prod

# Verify creation
kubectl get secrets -n wolf-blockchain-prod
kubectl describe secret wolf-blockchain-secrets -n wolf-blockchain-prod
```

#### Production Deployment with Secret Mount
```yaml
spec:
  containers:
  - name: api
    image: wolfblockchain:latest
    env:
    - name: Jwt__Secret
      valueFrom:
        secretKeyRef:
          name: wolf-blockchain-secrets
          key: Jwt__Secret
    - name: Security__AdminPassword
      valueFrom:
        secretKeyRef:
          name: wolf-blockchain-secrets
          key: Security__AdminPassword
    - name: ConnectionStrings__DefaultConnection
      valueFrom:
        secretKeyRef:
          name: wolf-blockchain-secrets
          key: ConnectionStrings__DefaultConnection
    # Mount secret as files (alternative approach)
    volumeMounts:
    - name: secrets
      mountPath: /app/secrets
      readOnly: true
  volumes:
  - name: secrets
    secret:
      secretName: wolf-blockchain-secrets
```

---

## Secrets Rotation

### Quarterly Rotation (Every 90 Days)

#### JWT Secret Rotation
```bash
# 1. Generate new secret
$newSecret = [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes((Get-Random -Count 32)))

# 2. Update staging
kubectl patch secret wolf-blockchain-secrets -n wolf-blockchain-staging -p '{"data":{"Jwt__Secret":"'$newSecret'"}}'

# 3. Test in staging (old tokens should still work for grace period)
# Run tests, verify no auth issues

# 4. Update production
kubectl patch secret wolf-blockchain-secrets -n wolf-blockchain-prod -p '{"data":{"Jwt__Secret":"'$newSecret'"}}'

# 5. Rolling restart to apply new secret
kubectl rollout restart deployment/wolf-blockchain-api -n wolf-blockchain-prod

# 6. Verify no auth failures (monitor logs)
kubectl logs -f deployment/wolf-blockchain-api -n wolf-blockchain-prod
```

#### Database Password Rotation
```bash
# 1. Change password in database
# sqlcmd -S server -U sa -P oldPassword -Q "ALTER LOGIN sa WITH PASSWORD='newPassword'"

# 2. Update K8s secret
kubectl patch secret wolf-blockchain-secrets \
  -n wolf-blockchain-prod \
  -p '{"data":{"ConnectionStrings__DefaultConnection":"base64-encoded-new-connection-string"}}'

# 3. Rolling restart
kubectl rollout restart deployment/wolf-blockchain-api -n wolf-blockchain-prod

# 4. Verify connections
kubectl logs -f deployment/wolf-blockchain-api -n wolf-blockchain-prod | grep -i connection
```

---

## Security Best Practices

### DO
- ✅ Use environment variables for all secrets
- ✅ Store secrets in K8s Secrets (at minimum)
- ✅ Use RBAC to limit secret access
- ✅ Enable K8s secret encryption at rest
- ✅ Rotate secrets quarterly
- ✅ Log all secret access
- ✅ Use different secrets per environment
- ✅ Use strong, random secret values (min 32 chars)
- ✅ Never commit secrets to Git
- ✅ Mask secrets in logs

### DON'T
- ❌ Don't hardcode secrets in code
- ❌ Don't use same secret across environments
- ❌ Don't commit appsettings.Production.json
- ❌ Don't pass secrets on command line
- ❌ Don't share secrets via email/Slack
- ❌ Don't use weak secrets
- ❌ Don't log secrets
- ❌ Don't commit .env files
- ❌ Don't share kubeconfig with secrets
- ❌ Don't keep old secrets after rotation

---

## Automation Scripts

### Rotate All Secrets (Script)
```powershell
# rotate-secrets.ps1

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("staging", "production")]
    [string]$Environment
)

$namespace = "wolf-blockchain-$Environment"

Write-Host "🔐 Starting secrets rotation for $Environment..." -ForegroundColor Cyan

# 1. Generate new secrets
$newJwtSecret = [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes([Guid]::NewGuid().ToString() + [Guid]::NewGuid().ToString()))
$newAdminPassword = [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes((New-Guid).ToString()))

# 2. Create backup of current secret
Write-Host "📦 Creating backup of current secrets..." -ForegroundColor Yellow
kubectl get secret wolf-blockchain-secrets -n $namespace -o yaml > "secret-backup-$Environment-$(Get-Date -Format yyyyMMdd-HHmmss).yaml"

# 3. Update secret
Write-Host "🔄 Updating secrets in $namespace..." -ForegroundColor Yellow
kubectl patch secret wolf-blockchain-secrets -n $namespace -p "{`"stringData`":{`"Jwt__Secret`":`"$newJwtSecret`"}}"

# 4. Rolling restart
Write-Host "♻️  Restarting deployment..." -ForegroundColor Yellow
kubectl rollout restart deployment/wolf-blockchain-api -n $namespace

# 5. Wait for rollout
kubectl rollout status deployment/wolf-blockchain-api -n $namespace --timeout=5m

# 6. Verify
Write-Host "✅ Secrets rotated successfully!" -ForegroundColor Green
Write-Host "Backup saved. Monitor logs: kubectl logs -f deployment/wolf-blockchain-api -n $namespace"
```

---

## Checklist for Deployment

- [ ] All secrets removed from source code
- [ ] .gitignore prevents accidental commits
- [ ] Environment variables used for all configuration
- [ ] K8s secrets created for each environment
- [ ] RBAC configured (who can access secrets)
- [ ] Secret rotation schedule established (quarterly)
- [ ] Audit logging enabled for secret access
- [ ] Backup procedure in place
- [ ] Team trained on secrets handling
- [ ] Incident response plan for secret compromise

---

## If Secret is Compromised

**Immediate actions (within 1 minute):**

1. Invalidate compromised secret
   ```bash
   kubectl delete secret wolf-blockchain-secrets -n wolf-blockchain-prod
   ```

2. Create new secret
   ```bash
   kubectl create secret generic wolf-blockchain-secrets \
     --from-literal=Jwt__Secret='new-emergency-secret-32chars' \
     -n wolf-blockchain-prod
   ```

3. Rolling restart
   ```bash
   kubectl rollout restart deployment/wolf-blockchain-api -n wolf-blockchain-prod
   ```

4. Notify team & management
5. Run security audit
6. Check logs for unauthorized access
7. Consider forced password resets for affected users

---

**This strategy ensures secrets are never exposed while remaining accessible to the application.**
