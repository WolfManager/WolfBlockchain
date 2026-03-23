# Plan pentru mâine — 2026-03-09

## Obiectiv principal
Trecere de la "code complete + tests green" la "release/deploy execution" controlat.

## Plan (ordonat)
1. **Preflight tehnic (15 min)**
   - rulează `dotnet build`
   - rulează `dotnet test tests/WolfBlockchain.Tests/WolfBlockchain.Tests.csproj`
   - confirmă status verde

2. **Config producție & secrete (30 min)**
   - validează `WOLF_TOKEN_SECRET` (>=32 chars)
   - verifică `WOLF_ALLOW_LEGACY_TOKEN` (dezactivat în prod)
   - verifică `appsettings.Production.json`

3. **Artefact release (30-45 min)**
   - build imagine Docker
   - tag versiune release
   - smoke run local/container

4. **Deploy Path A (60-90 min)**
   - deploy în mediu țintă
   - health-check endpoint-uri
   - verificare DB/cache/auth

5. **Post-deploy validation (30 min)**
   - verifică dashboard/monitoring
   - verifică logs + alerting
   - rulează smoke tests funcționale

6. **Checkpoint de final zi (10 min)**
   - creează fișier checkpoint nou cu:
     - status deploy,
     - rollback notes,
     - resume point pentru ziua următoare.

## Definition of Done (mâine)
- Build/Test: ✅
- Deploy: ✅
- Health checks: ✅
- Monitoring/alerts: ✅
- End-of-day checkpoint: ✅

## Comenzi rapide
```powershell
dotnet build
dotnet test tests/WolfBlockchain.Tests/WolfBlockchain.Tests.csproj
```
