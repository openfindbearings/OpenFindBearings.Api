# OpenFindBearings.Api — K3s 部署指南

## 前置条件

```bash
# 确认命名空间
kubectl get ns openfindbearings

# 确认 GHCR 拉取 Secret
kubectl get secret ghcr-pull-secret -n openfindbearings
```

## 部署步骤

```bash
# 1. Secret
kubectl apply -f deploy/k3s/secret.yml

# 2. 部署全部（ConfigMap + Deployment + Service + Ingress）
kubectl apply -k deploy/k3s/

# 3. 验证
kubectl get pods -n openfindbearings -l app=openfindbearings-api
curl -k https://api.515813.xyz/health/live
```

## 资源清单

| 文件 | 类型 | 说明 |
|------|------|------|
| `deploy.yml` | Deployment + Service + Ingress | API 服务 |
| `configMap.yml` | ConfigMap | Auth、Cache、日志等级等配置 |
| `secret.yml` | Secret | 数据库连接串（不提交 Git） |
| `secret-template.yml` | Secret 模板 | 占位符模板 |

## 镜像

`ghcr.io/openfindbearings/openfindbearings-api:v1.0.0`

## 镜像发布

```bash
gh workflow run deploy.yml -R openfindbearings/OpenFindBearings.Api --field tag=v1.0.0
```

或通过 GitHub Release 自动触发。
