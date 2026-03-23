# Secrets Setup Guide for Development

## Quick Start (First Time Only)

### 1. Initialize User Secrets
```powershell
cd src/WolfBlockchain.API
dotnet user-secrets init
```

### 2. Set Your Development Secrets
```powershell
# JWT Secret (copy-paste one value, use at least 32 characters)
dotnet user-secrets set "Jwt:Secret" "your-dev-secret-at-least-32-characters-long"

# Database Connection (if using custom DB)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=WolfBlockchain;Integrated Security=true;"

# Admin Bootstrap Token (for first-time setup)
dotnet user-secrets set "Security:BootstrapToken" "your-bootstrap-token-change-this"
```

### 3. Verify Secrets Are Set
```powershell
dotnet user-secrets list
```

You should see:
```
ConnectionStrings:DefaultConnection = Server=localhost;...
Jwt:Secret = your-dev-secret-at-least-32-characters-long
Security:BootstrapToken = your-bootstrap-token-change-this
```

### 4. Run Application
```powershell
dotnet run
```

The application will now:
- ✅ Use your user secrets automatically
- ✅ NOT commit them to Git
- ✅ Work in your IDE with Ctrl+F5 (Debug)

---

## Common Issues

### "JWT:Secret must be at least 32 characters"
**Solution**: Generate a new secret and set it:
```powershell
# Generate random secret
$secret = [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes(([Guid]::NewGuid().ToString() + [Guid]::NewGuid().ToString())))
Write-Host $secret

# Set it
dotnet user-secrets set "Jwt:Secret" $secret
```

### "Unable to decrypt token. Jwt:Secret not found"
**Solution**: Verify secrets are set:
```powershell
dotnet user-secrets list
# If empty, run Step 2 above again
```

### Secrets Not Updating
**Solution**: Restart Visual Studio or your terminal:
```powershell
# In terminal
dotnet user-secrets remove "Jwt:Secret"
dotnet user-secrets set "Jwt:Secret" "new-secret-here"

# Restart app
```

### "Secret ID not configured"
**Solution**: Run initialize:
```powershell
dotnet user-secrets init
```

---

## Where Secrets Are Stored

Your secrets are stored in:
```
Windows:  %APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json
macOS:    ~/.microsoft/usersecrets/<UserSecretsId>/secrets.json
Linux:    ~/.microsoft/usersecrets/<UserSecretsId>/secrets.json
```

**They are automatically encrypted by your OS.**

---

## Security Reminders

- ✅ **DO**: Use different secrets per environment
- ✅ **DO**: Keep secrets.json out of Git (it's in .gitignore)
- ✅ **DO**: Share secrets via secure channel (1password, LastPass, etc)
- ✅ **DO**: Rotate production secrets every 90 days

- ❌ **DON'T**: Commit secrets to Git
- ❌ **DON'T**: Share secrets via email/Slack/Teams
- ❌ **DON'T**: Use same secret across environments
- ❌ **DON'T**: Log secrets (they're masked in logs)
- ❌ **DON'T**: Put secrets in appsettings.json files

---

## For Production/Staging Deployment

### Staging (Kubernetes)
```bash
# Create secret from the secrets management tool
kubectl create secret generic wolf-blockchain-secrets \
  --from-literal=Jwt__Secret='staging-jwt-secret-32chars' \
  --from-literal=Security__BootstrapToken='staging-bootstrap-token' \
  -n wolf-blockchain-staging
```

### Production (Kubernetes)
```bash
# Create secret with encryption
kubectl create secret generic wolf-blockchain-secrets \
  --from-literal=Jwt__Secret='prod-jwt-secret-32chars' \
  --from-literal=Security__BootstrapToken='prod-bootstrap-token' \
  -n wolf-blockchain-prod
```

See `SECRETS_MANAGEMENT_STRATEGY.md` for complete production setup.

---

## Team Onboarding

When a new developer joins:

1. **Clone the repo** (no secrets included)
2. **Run**: `dotnet user-secrets init`
3. **Get secrets from**: [Your SecurePass tool/manager]
4. **Set secrets locally**: Use commands in "Quick Start" section
5. **Test**: `dotnet run` should start without errors

---

## Support

- **Secrets not working?** Check `SECRETS_MANAGEMENT_STRATEGY.md`
- **Production deployment?** See deployment runbook
- **Questions?** Ask the DevOps team

---

**Never commit secrets. Never hardcode credentials. You're secure!** 🔐
