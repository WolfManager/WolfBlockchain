# ============================================
# 🐺 WOLF BLOCKCHAIN - GITHUB SECRETS SETUP
# ============================================
# Guide for configuring required GitHub secrets

## Required Secrets

Add these secrets in GitHub repository settings:
Settings → Secrets and variables → Actions → New repository secret

### Docker Hub Authentication
```
DOCKER_USERNAME=your-dockerhub-username
DOCKER_PASSWORD=your-dockerhub-token-or-password
```

### JWT Configuration
```
JWT_SECRET=WolfBlockchainProductionSecretKey2024MinimumThirtyTwoCharacters!
```

### SonarCloud (Optional - for code quality)
```
SONAR_TOKEN=your-sonarcloud-token
```

### Database Configuration (for deployment)
```
DB_CONNECTION_STRING=Server=your-server;Database=WolfBlockchainDb;User=sa;Password=your-password
```

### Notification Services (Optional)
```
SLACK_WEBHOOK_URL=https://hooks.slack.com/services/YOUR/WEBHOOK/URL
TEAMS_WEBHOOK_URL=https://outlook.office.com/webhook/YOUR/WEBHOOK/URL
```

---

## How to Get Docker Hub Token

1. Go to https://hub.docker.com/settings/security
2. Click "New Access Token"
3. Name: "GitHub Actions CI/CD"
4. Permissions: Read, Write, Delete
5. Copy token and add as `DOCKER_PASSWORD` secret

---

## How to Get SonarCloud Token

1. Go to https://sonarcloud.io/account/security
2. Generate new token
3. Add as `SONAR_TOKEN` secret

---

## Environment-Specific Variables

### Staging
```
STAGING_URL=https://staging.wolfblockchain.com
STAGING_DB_CONNECTION=Server=staging-db;Database=WolfBlockchainDb;...
```

### Production
```
PRODUCTION_URL=https://wolfblockchain.com
PRODUCTION_DB_CONNECTION=Server=prod-db;Database=WolfBlockchainDb;...
```

---

## Security Best Practices

1. ✅ Never commit secrets to repository
2. ✅ Use separate secrets for staging/production
3. ✅ Rotate secrets regularly (every 90 days)
4. ✅ Use minimal permissions for tokens
5. ✅ Enable secret scanning on GitHub
6. ✅ Review secret access logs regularly

---

## Testing GitHub Actions Locally

Use `act` tool to test workflows locally:
```bash
# Install act
brew install act  # macOS
choco install act  # Windows

# Test workflow
act -j build-and-test

# Test with secrets
act -j docker-build --secret-file .secrets
```

---

## Webhook URLs for Notifications

### Slack
```
Settings → Your Workspace → Manage Apps → Custom Integrations → Incoming Webhooks
```

### Microsoft Teams
```
Teams Channel → Connectors → Incoming Webhook → Configure
```

---

**After adding secrets, the CI/CD pipeline will run automatically on push!** 🚀
