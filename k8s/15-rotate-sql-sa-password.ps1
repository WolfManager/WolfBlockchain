param(
    [Parameter(Mandatory = $true)]
    [string]$CurrentSaPassword,

    [Parameter(Mandatory = $true)]
    [string]$NewSaPassword,

    [string]$Namespace = "wolf-blockchain",
    [string]$DatabaseName = "WolfBlockchainDb",
    [string]$SqlServiceName = "wolf-blockchain-db"
)

if ($NewSaPassword.Length -lt 12) {
    throw "New SA password must be at least 12 characters."
}

$tempPod = "sqlcmd-rotator"

# Create an ephemeral tools pod that has sqlcmd available
kubectl -n $Namespace delete pod $tempPod --ignore-not-found | Out-Null
kubectl -n $Namespace run $tempPod --image=mcr.microsoft.com/mssql-tools --restart=Never --command -- sleep 3600 | Out-Null
kubectl -n $Namespace wait --for=condition=Ready pod/$tempPod --timeout=120s | Out-Null

try {
    $escapedNewPassword = $NewSaPassword.Replace("'", "''")
    $alterLoginQuery = "ALTER LOGIN [sa] WITH PASSWORD = N'$escapedNewPassword';"

    kubectl exec -n $Namespace $tempPod -- /opt/mssql-tools/bin/sqlcmd -S "$SqlServiceName,1433" -U sa -P "$CurrentSaPassword" -Q "$alterLoginQuery"
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to rotate SA password with sqlcmd via tools pod."
    }

    kubectl exec -n $Namespace $tempPod -- /opt/mssql-tools/bin/sqlcmd -S "$SqlServiceName,1433" -U sa -P "$NewSaPassword" -Q "SELECT 1"
    if ($LASTEXITCODE -ne 0) {
        throw "Validation with new SA password failed."
    }

    $connectionString = "Server=$SqlServiceName;Database=$DatabaseName;User=sa;Password=$NewSaPassword;Encrypt=true;TrustServerCertificate=true;Connection Timeout=30;MultipleActiveResultSets=true;"

    $saPasswordBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($NewSaPassword))
    $connectionStringBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($connectionString))

    $secretPatch = @{ data = @{ SA_PASSWORD = $saPasswordBase64; "ConnectionStrings__DefaultConnection" = $connectionStringBase64 } } | ConvertTo-Json -Compress
    $patchFile = [System.IO.Path]::GetTempFileName()
    [System.IO.File]::WriteAllText($patchFile, $secretPatch, [System.Text.UTF8Encoding]::new($false))

    kubectl patch secret wolf-blockchain-secrets -n $Namespace --type merge --patch-file $patchFile
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to patch wolf-blockchain-secrets with rotated SA password."
    }

    Remove-Item $patchFile -Force -ErrorAction SilentlyContinue

    kubectl -n $Namespace rollout restart deployment/wolf-blockchain-api
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to restart API deployment after secret update."
    }

    kubectl -n $Namespace rollout status deployment/wolf-blockchain-api --timeout=180s
    if ($LASTEXITCODE -ne 0) {
        throw "API rollout did not complete after secret update."
    }

    kubectl -n $Namespace get secret wolf-blockchain-secrets -o jsonpath="{.metadata.name} updated"
}
finally {
    kubectl -n $Namespace delete pod $tempPod --ignore-not-found | Out-Null
}
