# deploy（OpenFindBearings.Api 部署模板）

本目录是 API 的 K8s 部署清单模板，供自部署使用。**所有真实域名/集群细节已脱敏为占位符**，
替换占位符并创建 Secret 后即可 `kubectl apply`。

## 前置

1. 命名空间：`openfindbearings`（或自行修改所有清单里的 `namespace`）
2. 镜像：`ghcr.io/openfindbearings/openfindbearings-api`（公开可拉）

## 步骤

1. **创建 Secret**：`secrets/api-secret-template.yml` 填真实值（连接串、Redis 串、内部令牌、MinIO 账号）后 apply
2. **替换占位符**：
   - `<your-identity-domain>`：Identity 部署域名（configMap.yml）
   - `<your-site-domain>`：站点访问域名（configMap.yml AllowedOrigins）
   - `<your-pod-cidr>` / `<your-service-cidr>`：你集群的 Pod/Service 网段（deploy.yml，按自己集群实际值填）
   - `<your-bff-domain>`：BFF/媒体服务域名（media-server.yml Ingress）
3. **依赖**：Identity（认证）、MinIO（对象存储，minio.yml 自带）、可选 Redis（`CacheSettings__EnableRedis=true` 时需 Redis；无 Redis 可置 `"false"`，事件链降级）

## apply 顺序

```
minio.yml → configMap.yml → deploy.yml → media-server.yml
```

> 完整运维清单（真实域名/CIDR/密钥）在私有运维库，本目录只提供模板，占位符请在部署时替换为真实值。
