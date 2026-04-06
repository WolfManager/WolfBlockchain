# Kubernetes Prerequisites

Before applying the manifests in this directory, ensure the following are in place.

## Required tools

- `kubectl` >= 1.28 connected to the target cluster
- `helm` >= 3.12 (for cert-manager / ingress-nginx installation)

## Ingress controller

```bash
helm upgrade --install ingress-nginx ingress-nginx \
  --repo https://kubernetes.github.io/ingress-nginx \
  --namespace ingress-nginx --create-namespace
```

## cert-manager

```bash
helm upgrade --install cert-manager cert-manager \
  --repo https://charts.jetstack.io \
  --namespace cert-manager --create-namespace \
  --set installCRDs=true
```

## Apply manifests in order

```bash
kubectl apply -f k8s/01-namespace.yaml
kubectl apply -f k8s/02-configmap.yaml
kubectl apply -f k8s/03-secret.yaml   # fill real secrets first!
kubectl apply -f k8s/04-pvc.yaml
kubectl apply -f k8s/05-statefulset-db.yaml
kubectl apply -f k8s/06-services.yaml
kubectl apply -f k8s/07-deployment.yaml
kubectl apply -f k8s/08-hpa.yaml
kubectl apply -f k8s/09-ingress.yaml
kubectl apply -f k8s/10-prometheus-config.yaml
kubectl apply -f k8s/11-prometheus-deployment.yaml
```

> ⚠️ Edit `k8s/03-secret.yaml` and replace every `__SET_IN_CLUSTER_ONLY__`
> placeholder with real base64-encoded values before applying.