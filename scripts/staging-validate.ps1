param(
    [string]$ApiUrl = "http://localhost",
    [string]$IngressHost = "wolf-blockchain.local",
    [string]$Namespace = "wolf-blockchain"
)

Write-Host "== WolfBlockchain Staging Validation ==" -ForegroundColor Cyan
Write-Host "Namespace: $Namespace"
Write-Host "API URL: $ApiUrl"
Write-Host "Ingress Host: $IngressHost"

Write-Host "`n1) Deployment rollout status" -ForegroundColor Yellow
kubectl rollout status deployment/wolf-blockchain-api -n $Namespace --timeout=300s
if ($LASTEXITCODE -ne 0) { throw "Deployment rollout failed." }

Write-Host "`n2) Pods status" -ForegroundColor Yellow
kubectl get pods -n $Namespace -l component=api

Write-Host "`n3) Health endpoint" -ForegroundColor Yellow
$health = curl.exe -s -o NUL -w "%{http_code}" -H "Host: $IngressHost" "$ApiUrl/health"
Write-Host "Health HTTP: $health"
if ($health -ne "200") { throw "Health endpoint failed with HTTP $health" }

Write-Host "`n4) Metrics endpoint" -ForegroundColor Yellow
$metrics = curl.exe -s -o NUL -w "%{http_code}" -H "Host: $IngressHost" "$ApiUrl/metrics"
Write-Host "Metrics HTTP: $metrics"
if ($metrics -ne "200") { throw "Metrics endpoint failed with HTTP $metrics" }

Write-Host "`n5) Smoke tests" -ForegroundColor Yellow
bash scripts/smoke-tests.sh $ApiUrl 3 $IngressHost
if ($LASTEXITCODE -ne 0) { throw "Smoke tests failed." }

Write-Host "`n✅ Staging validation passed." -ForegroundColor Green