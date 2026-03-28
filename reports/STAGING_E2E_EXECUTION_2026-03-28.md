# Staging Deployment + E2E Execution Report

Date: 2026-03-28
Workspace: `D:\WolfBlockchain`
Environment checked: local staging cluster (`docker-desktop`, namespace `wolf-blockchain`)

## 1) Pipeline trigger

- Ran `scripts/push-main-staging.ps1` successfully.
- `main` pushed to `origin/main`.
- `staging` force-updated from `main` and pushed with empty trigger commit.

## 2) Staging rollout health

- Deployment rollout: PASS
- Pods: 3/3 Ready
- Health endpoint via ingress host: `200`
- Metrics endpoint via ingress host: `200`
- `scripts/staging-validate.ps1`: PASS

## 3) Logs / ingress / probes

- Ingress present: `wolf-blockchain-ingress` (`wolf-blockchain.local`)
- Repeated successful `/health` and `/metrics` requests in pod logs.
- Rollout restart test: PASS (deployment recovered to ready).
- Rollback path: verified with `kubectl rollout undo ... --dry-run`.

## 4) Secrets / variables / environment config

### Repository-level CI/CD config
- Local preflight script PASS, but GitHub API writes are blocked locally:
  - `gh` CLI missing
  - `GITHUB_TOKEN` missing
  - direct API calls return `401 Unauthorized`

### Cluster-level runtime config
- `wolf-blockchain-secrets` contains DB/JWT/token keys.
- Added explicit placeholders in `k8s/03-secret.yaml` for:
  - `Blockchain__RpcApiKey`, `Wallet__KeystorePassword`, `AI__ApiKey`, `Cache__RedisPassword`, `Queue__ConnectionString`
- Added explicit non-secret env config in `k8s/02-configmap.yaml` for:
  - `Blockchain__RpcUrl`, `AI__Endpoint`, `Queue__Provider`, etc.

## 5) E2E functional validation

### Deterministic auth note
With 3 replicas, owner/user state is in-memory/static per pod, so auth/bootstrap requests are not deterministic across pods.

### Controlled E2E check
- Temporarily scaled deployment to 1 replica for deterministic validation.
- Bootstrap owner: PASS
- Login + JWT issuance: PASS
- Request flow PASS:
  - `POST /api/wallet/create` (x2)
  - `POST /api/blockchain/transaction`
  - `POST /api/blockchain/mine`
  - `GET /api/blockchain/balance/{address}`

## 6) Persistence and restart behavior

- Added writable mount for `/app/data` in deployment (fixes runtime write failures under `readOnlyRootFilesystem`).
- Replaced `/app/data` `emptyDir` with PVC `wolf-blockchain-api-data-pvc`.
- Confirmed `save -> rollout restart -> load` works:
  - pre-restart save: PASS
  - post-restart load: PASS (`TotalBlocks=2` in controlled single-replica validation)

## 7) Blockchain RPC / external integrations

- Env keys for RPC/AI/queue are now present in pod.
- Current values are placeholders/defaults (`https://rpc.your-network.tld`, etc.).
- Current code paths do not yet implement real outbound blockchain RPC provider calls for production network validation.

## 8) Critical findings to resolve before production

1. **CI/CD secret automation blocked locally**
   - Install `gh` or provide `GITHUB_TOKEN` with repo/actions write scope.

2. **Auth/state not shared across replicas**
   - Move user/auth state from static in-memory to shared persistent store (DB/distributed cache).

3. **RPC/network integration not production-validated**
   - Add/validate real RPC client integration and network connectivity checks.

## 9) Current verdict

- **Staging infra health**: PASS
- **Smoke validation**: PASS
- **E2E flow**: PASS under single-replica controlled test
- **Data persistence across restart**: PASS (with PVC-backed `/app/data`)
- **Production readiness**: NOT YET (due to shared-state and real RPC validation gaps)

## 10) Shared auth-state stabilization update

- Implemented persistent user-state synchronization in `src/WolfBlockchain.Core/UserManager.cs`:
  - state persisted to `users-state.json` under `/app/data`
  - live instances auto-reload state when file timestamp changes
  - synchronized in-process access with lock
- Added regression tests in `tests/WolfBlockchain.Tests/Core/UserManagerPersistenceTests.cs`:
  - state survives manager recreation
  - password changes persist
  - running second manager observes updates from first manager
- Tests for the new behavior: PASS (`3/3`)
- Built and deployed API image `wolfblockchain:v2.0.1-local` to staging deployment.
- Post-deploy staging validation (`scripts/staging-validate.ps1`): PASS (3/3 pods ready, health/metrics 200).

## 11) Multi-replica auth consistency validation (post-fix)

- Deployed image with UserManager persistence/reload fix: `wolfblockchain:v2.0.1-local`.
- Bootstrapped owner once, then validated login on each API pod directly (localhost inside pod):
  - pod 1: login `200`, `accessToken` present
  - pod 2: login `200`, `accessToken` present
  - pod 3: login `200`, `accessToken` present
- Verified shared state file on all pods:
  - `/app/data/users-state.json` exists on each pod
  - identical SHA-256 hash across all pods

Result: auth state is now consistent across replicas in staging after the persistence + reload change.

## 12) Hardening rollout: secrets + probes + readiness unblock

- Sensitive config handling hardened:
  - removed sensitive RPC and key-patterned values from ConfigMap
  - mapped sensitive values via `secretKeyRef` in deployment env
  - removed `secretRef` from `envFrom` to avoid broad implicit secret import
- Security context hardened in deployment:
  - `automountServiceAccountToken: false`
  - non-root runtime user/group set to `10001`
  - `readOnlyRootFilesystem: true` preserved with write access limited to mounted volumes (`/app/data`, `/app/logs`, `/tmp`)
- Added dedicated readiness path `/ready` and switched `readinessProbe` to it.
- Rollout blocker fixed:
  - added explicit bypass for `/ready` in `AdminIpAllowlistMiddleware`
  - added explicit bypass for `/ready` in `RateLimitingMiddleware`
  - rollout now completes cleanly (`READY=3`, `UPDATED=3`, `REPLICAS=3`)

## 13) Validation summary after hardening changes

- Staging rollout status: PASS
- Staging validation script: PASS
- `/health`: 200
- `/ready`: 200
- `/metrics`: 200
- Pod readiness: 3/3 ready, no blocked NotReady pod after middleware bypass fix
- Build: PASS
- Tests (non-integration/non-performance): PASS (`156/156`)

## 14) Manual production-only actions (must be executed outside repo)

1. Replace all `__SET_IN_CLUSTER_ONLY__` placeholder values in `wolf-blockchain-secrets` with real secrets.
2. Set real RPC endpoints:
   - `RPC_PRIMARY`
   - `RPC_FALLBACK`
   - optional `RPC_AUTH_TOKEN` (if upstream requires auth)
3. Verify no secret values are printed in logs during startup and failover events.
4. Re-run production go-live checklist before promotion.
