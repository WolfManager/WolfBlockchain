param(
    [string]$Namespace = "wolf-blockchain"
)

$ErrorActionPreference = "Stop"

kubectl version --client | Out-Null
kubectl cluster-info | Out-Null

kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml

$metricsPatch = @'
{
  "spec": {
    "template": {
      "spec": {
        "containers": [
          {
            "name": "metrics-server",
            "args": [
              "--cert-dir=/tmp",
              "--secure-port=10250",
              "--kubelet-preferred-address-types=InternalIP,ExternalIP,Hostname",
              "--kubelet-use-node-status-port",
              "--metric-resolution=15s",
              "--kubelet-insecure-tls"
            ]
          }
        ]
      }
    }
  }
}
'@

kubectl -n kube-system patch deployment metrics-server --type strategic -p $metricsPatch
kubectl -n kube-system rollout status deployment/metrics-server --timeout=180s

kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/main/deploy/static/provider/cloud/deploy.yaml
kubectl -n ingress-nginx rollout status deployment/ingress-nginx-controller --timeout=300s

if (kubectl get namespace $Namespace --no-headers 2>$null) {
    kubectl apply -f k8s/09-ingress.yaml
    kubectl get ingress -n $Namespace
    kubectl get hpa -n $Namespace
}

kubectl top nodes
kubectl get ingressclass