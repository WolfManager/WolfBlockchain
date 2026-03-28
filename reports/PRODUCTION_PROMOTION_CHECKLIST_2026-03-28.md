# Production Promotion Checklist

Date: 2026-03-28

## Freeze & release
- [ ] Freeze code on `main`
- [ ] Tag release (`v2.0.0` or next)
- [ ] Confirm commit hash to promote

## Backup & rollback
- [ ] DB backup created and verified
- [ ] Secret backup/export secured
- [ ] Rollback command tested (`kubectl rollout undo`)
- [ ] Previous container image digest noted

## CI/CD configuration
- [ ] GitHub secrets set (`DOCKER_USERNAME`, `DOCKER_PASSWORD`, `KUBE_CONFIG_STAGING`, `KUBE_CONFIG_PROD`)
- [ ] GitHub variables set (`STAGING_NAMESPACE`, `PRODUCTION_NAMESPACE`)
- [ ] Production environment protections active

## Runtime security and config
- [ ] Production `Jwt__Secret` rotated and validated
- [ ] Production `ConnectionStrings__DefaultConnection` validated
- [ ] Production `Security__AllowedOrigins` validated
- [ ] Admin IP allowlist validated

## Go-live checks
- [ ] Staging E2E passed on production-like config
- [ ] Deployment rollout healthy in production
- [ ] `/health` and `/metrics` return 200
- [ ] Error rate and latency within threshold
- [ ] Alerting pipeline active (Slack/Webhook or alternative)

## Post-go-live monitoring window (first 30-60 min)
- [ ] Pod restarts stable
- [ ] Logs reviewed for auth/storage errors
- [ ] API success-rate validated on real traffic
- [ ] Decision logged: Go / No-Go / Rollback
