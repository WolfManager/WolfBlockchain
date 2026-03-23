param(
    [Parameter(Mandatory = $true)]
    [string]$ApiDomain,

    [string]$Namespace = "wolf-blockchain",
    [string]$TlsSecretName = "wolf-blockchain-tls",
    [string]$ClusterIssuer = "letsencrypt-prod",
    [string]$ClusterIssuerManifestPath = "k8s/13-clusterissuer-letsencrypt-prod.yaml"
)

$ErrorActionPreference = "Stop"

if (-not (kubectl get namespace $Namespace --no-headers 2>$null)) {
    throw "Namespace '$Namespace' not found. Deploy base manifests first."
}

if (-not (kubectl get configmap wolf-blockchain-config -n $Namespace --no-headers 2>$null)) {
    throw "ConfigMap 'wolf-blockchain-config' not found in namespace '$Namespace'."
}

if (-not (kubectl get ingress wolf-blockchain-ingress -n $Namespace --no-headers 2>$null)) {
    throw "Ingress 'wolf-blockchain-ingress' not found in namespace '$Namespace'."
}

$apiUrl = "https://$ApiDomain"
$configMapPatchFile = [System.IO.Path]::GetTempFileName()
$ingressPatchFile = [System.IO.Path]::GetTempFileName()

try {
    if (kubectl get crd clusterissuers.cert-manager.io --no-headers 2>$null) {
        if (Test-Path $ClusterIssuerManifestPath) {
            kubectl apply -f $ClusterIssuerManifestPath | Out-Null
        }
    }
    else {
        Write-Host "cert-manager CRD is not installed. Skipping ClusterIssuer apply."
        Write-Host "Install cert-manager first, then re-run this script for automatic TLS."
    }

    $configMapPatch = @{ data = @{ "WolfBlockchain__ApiUrl" = $apiUrl } } | ConvertTo-Json -Compress
    Set-Content -Path $configMapPatchFile -Value $configMapPatch -Encoding UTF8
    kubectl patch configmap wolf-blockchain-config -n $Namespace --type merge --patch-file $configMapPatchFile | Out-Null

    $ingressAnnotations = @{}
    if (kubectl get crd clusterissuers.cert-manager.io --no-headers 2>$null) {
        $ingressAnnotations["cert-manager.io/cluster-issuer"] = $ClusterIssuer
    }

    $ingressPatch = @{
        metadata = @{ annotations = $ingressAnnotations }
        spec = @{
            tls = @(@{ hosts = @($ApiDomain); secretName = $TlsSecretName })
            rules = @(@{
                host = $ApiDomain
                http = @{
                    paths = @(
                        @{ path = "/"; pathType = "Prefix"; backend = @{ service = @{ name = "wolf-blockchain-api"; port = @{ number = 5000 } } } },
                        @{ path = "/health"; pathType = "Exact"; backend = @{ service = @{ name = "wolf-blockchain-api"; port = @{ number = 5000 } } } },
                        @{ path = "/metrics"; pathType = "Exact"; backend = @{ service = @{ name = "wolf-blockchain-api"; port = @{ number = 5000 } } } },
                        @{ path = "/swagger"; pathType = "Prefix"; backend = @{ service = @{ name = "wolf-blockchain-api"; port = @{ number = 5000 } } } }
                    )
                }
            })
        }
    } | ConvertTo-Json -Compress -Depth 20

    Set-Content -Path $ingressPatchFile -Value $ingressPatch -Encoding UTF8
    kubectl patch ingress wolf-blockchain-ingress -n $Namespace --type merge --patch-file $ingressPatchFile | Out-Null

    kubectl -n $Namespace rollout restart deployment/wolf-blockchain-api | Out-Null
    kubectl -n $Namespace rollout status deployment/wolf-blockchain-api --timeout=180s
    kubectl -n $Namespace get ingress wolf-blockchain-ingress
}
finally {
    if (Test-Path $configMapPatchFile) { Remove-Item $configMapPatchFile -Force }
    if (Test-Path $ingressPatchFile) { Remove-Item $ingressPatchFile -Force }
}
