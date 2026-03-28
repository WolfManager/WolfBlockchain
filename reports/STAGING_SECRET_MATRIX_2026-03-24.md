# Staging Secret/Variable Matrix (Audit)

Date: 2026-03-24
Repo: `https://github.com/WolfManager/WolfBlockchain`
Namespace: `wolf-blockchain`
Cluster context: `docker-desktop`

## 1) GitHub Actions requirements (`.github/workflows/deploy.yml`)

### Required repository secrets
- `DOCKER_USERNAME`
- `DOCKER_PASSWORD`
- `KUBE_CONFIG_STAGING`
- `KUBE_CONFIG_PROD`
- `SLACK_WEBHOOK` (optional)

### Required repository variables
- `STAGING_NAMESPACE` (default fallback: `wolf-blockchain`)
- `PRODUCTION_NAMESPACE` (default fallback: `wolf-blockchain`)

### Current verification status
- Local machine cannot verify or set GitHub secrets/variables automatically because:
  - `gh` CLI is not installed
  - `GITHUB_TOKEN` is not present in shell environment

## 2) Kubernetes runtime secrets/config present in cluster

### Secret `wolf-blockchain-secrets` keys found
- `ConnectionStrings__DefaultConnection`
- `Jwt__Secret`
- `SA_PASSWORD`
- `WOLF_TOKEN_SECRET`

### ConfigMap `wolf-blockchain-config` notable keys found
- `ASPNETCORE_ENVIRONMENT`
- `ASPNETCORE_URLS`
- `Jwt__ExpirationMinutes`
- `Jwt__RefreshTokenExpirationDays`
- `Security__*` security configuration keys
- `Monitoring__*` and `Performance__*` keys

## 3) Gap analysis for requested staging E2E scope

Requested scope includes secrets/config for blockchain node, wallet, RPC, DB, AI services.

### Present now
- DB connection secret: present
- JWT secret: present
- SQL SA password: present

### Missing as explicit runtime keys in cluster secret
- RPC endpoint URL/key (e.g. `Blockchain__RpcUrl`, `Blockchain__RpcApiKey`)
- Wallet external signer/keystore credentials (e.g. `Wallet__KeystorePath`, `Wallet__KeystorePassword`)
- AI provider credentials (e.g. `AI__Provider`, `AI__ApiKey`, `AI__Endpoint`)
- Cache/queue explicit credentials (if Redis/queue is used externally)

## 4) Immediate action list to close secrets/config readiness

1. Configure GitHub repository secrets listed above.
2. Configure GitHub variables for staging/production namespaces.
3. Extend `k8s/03-secret.yaml` with explicit keys for RPC/wallet/AI integrations.
4. Extend `k8s/02-configmap.yaml` with non-secret endpoint/environment keys.
5. Roll secrets into cluster and restart deployment.
