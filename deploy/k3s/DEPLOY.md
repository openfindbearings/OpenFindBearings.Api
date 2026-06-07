# OpenFindBearings.Api — 部署指南

## 部署方式

secret 含敏感数据不上传 Git。部署前先从模板创建：

```bash
# 1. 复制模板改值
cp deploy/k3s/secret-template.yml deploy/k3s/secret.yml
# 编辑填入真实数据库连接串和 Redis 连接串

# 2. 创建 secret（先于 kustomize）
kubectl apply -f deploy/k3s/secret.yml

# 3. 部署全部
kubectl apply -k deploy/k3s/
```

## 资源清单

| 文件 | 类型 | 说明 |
|------|------|------|
| `deploy.yml` | Deployment | API 服务 (1 Pod) |
| `configMap.yml` | ConfigMap | Auth、Cache、日志等级等配置 |

## 仓库策略

| 文件 | Git | 说明 |
|------|-----|------|
| `secret-template.yml` | 已提交 | 占位符模板 |
| `secret.yml` | `.gitignore` | 敏感数据不提交 |

## 镜像发布

通过 GitHub Actions `deploy.yml` 自动构建推送 `openfindbearings-api:latest`：

```
gh workflow run deploy.yml -R openfindbearings/OpenFindBearings.Api --field tag=v1.0.0
```

或通过 GitHub Release 自动触发。
