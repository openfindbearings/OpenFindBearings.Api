# MinIO 对象存储设计与迁移指南

- 版本：v1.0.0
- 日期：2026-09-20
- 状态：已实施（待集群部署）
- 适用：OpenFindBearings.Api / FindBearings.Sync / 媒体服务 的图片与文件存储统一改造

---

## 1. 背景与动机

改造前的图片存储是"读统一、写分散"：

```
改造前：
  Sync 爬虫图片  --写--> hostPath /opt/openfindbearings/sync/images  (initContainer chmod 777 hack)
  API 用户上传   --写--> hostPath /opt/openfindbearings/api/uploads  (initContainer chown 1654 hack)
  媒体服务       --读--> nginx 只读挂载上述两个 hostPath 直出
```

问题：hostPath 不支持 fsGroup 权限托管，只能靠 initContainer 手工 chown/chmod（曾引发
`Access to the path '/app/wwwroot/uploads' is denied` 线上故障）；数据生命周期散落在节点目录；
多节点调度即失效；无对象版本/生命周期管理。

改造后（S3 标准协议统一）：

```
改造后：
  Sync 爬虫图片  --S3--> MinIO bucket=media  key=images/**
  API 用户上传   --S3--> MinIO bucket=media  key=uploads/**
  媒体服务       --读--> nginx 反代 MinIO（URL 形态不变，客户端零改动）
  数据持久化     MinIO 自身 PVC（local-path，权限由 kubelet 管理）
```

选型要点：

- 应用侧统一走 **S3 协议**（客户端 AWS SDK for .NET，MinIO 官方推荐路径），
  将来切换云厂商 OSS/S3 仅改 Endpoint 与凭据，代码零改动
- 库内 URL 仍为相对键（`/uploads/...`、`/images/...`），展示层
  （Taro `getMediaBase` + 站点配置 `Mobile.MediaBaseUrl`）机制不变
- 开发环境保留 `LocalFile` Provider（写 wwwroot），配置一键切换

## 2. 代码落点

| 项目 | 位置 | 说明 |
|---|---|---|
| Sync | `Application/Services/IObjectStorageService.cs` | 既有抽象（Upload/Delete/GetUrl/Exists） |
| Sync | `Infrastructure/Services/ObjectStorage/MinioStorage.cs` | v17.6.0 实装（原 Task.Delay 空壳），key 前缀 `ImageProcessing:ObjectStorage:KeyPrefix`（默认 images） |
| Sync | `Infrastructure/Services/ObjectStorage/LocalFileStorage.cs` | 开发实现，保留 |
| Sync | `Infrastructure/DependencyInjection.cs` | `ImageProcessing:ObjectStorage:Provider` 选择；删除 AliyunOss 僵尸分支；`IAmazonS3` 单例（ForcePathStyle） |
| API | `Application/Services/IObjectStorageService.cs` | v1.5.0 新增（取代从未被消费的 IFileService 死代码） |
| API | `Infrastructure/Services/ObjectStorage/{MinioStorage,LocalFileStorage}.cs` | 与 Sync 同构；key 由调用方给定（uploads/**） |
| API | `Infrastructure/DependencyInjection.cs` | `FileStorage:Provider` 选择 |
| API | `Endpoints/{UserEndpoints,MerchantEndpoints}.cs` | 头像/Logo/证照材料/预上传 4 端点落盘改走抽象；`FileUploadHelper.ContentTypeFromExtension` 定 MIME |
| 部署 | `OpenFindBearings.Api/deploy/k3s/minio.yml` | MinIO 本体（Secret 模板 + PVC + Deployment + Service） |
| 部署 | `media-server.yml` | 两段 alias 改 proxy_pass MinIO |
| 部署 | API/Sync configMap + secret-template | Provider=Minio + Endpoint/Bucket + 应用凭据 |
| 部署 | API deploy.yml / Sync api-deployment / job-transform / job-images | hostPath images/uploads 卷与对应 initContainer hack 废弃（Sync 仅保留 Excel temp 卷） |

## 3. 部署步骤（服务器执行）

### 3.1 起 MinIO

```bash
# 1) 改 deploy/k3s/minio.yml 中 openfindbearings-minio-secrets 的 root 凭据（或用 kubectl 直接建）
kubectl apply -f minio.yml
kubectl rollout status deploy/openfindbearings-minio -n openfindbearings
```

### 3.2 建桶、应用账号、策略（一次性）

```bash
kubectl exec -n openfindbearings deploy/openfindbearings-minio -- sh -c '
  mc alias set local http://localhost:9000 "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD"
  # 桶（一次建好，API/Sync 共用）
  mc mb -p local/media
  # 匿名只读下载（媒体内容公开，供 nginx 反代直出）
  mc anonymous set download local/media
  # 应用写入账号（AccessKey 与下面 SecretKey 填进 API/Sync 的 K8s Secret）
  mc admin user add local ofb-app "<生成一个强随机密钥>"
  # 限桶读写策略
  cat > /tmp/ofb-media-rw.json <<EOF
{"Version":"2012-10-17","Statement":[{"Effect":"Allow","Action":["s3:GetObject","s3:PutObject","s3:DeleteObject","s3:ListBucket"],"Resource":["arn:aws:s3:::media","arn:aws:s3:::media/*"]}]}
EOF
  mc admin policy create local ofb-media-rw /tmp/ofb-media-rw.json
  mc admin policy attach local ofb-media-rw --user ofb-app
'
```

### 3.3 应用凭据与配置切换

```bash
# API：Secret 增加 FileStorage__AccessKey / FileStorage__SecretKey（值=ofb-app/上一步密钥）
kubectl -n openfindbearings edit secret openfindbearings-api-secrets
kubectl apply -f configMap.yml   # 已含 FileStorage__Provider=Minio
kubectl -n openfindbearings rollout restart deploy/openfindbearings-api

# Sync：Secret 增加 ImageProcessing__ObjectStorage__AccessKey / SecretKey
kubectl -n openfindbearings edit secret openfindbearings-sync-secrets
kubectl apply -f findbearings-sync-api-configmap.yml findbearings-sync-job-configmap.yml
kubectl -n openfindbearings rollout restart deploy/openfindbearings-sync-api
kubectl apply -f findbearings-sync-job-transform.yml findbearings-sync-job-images.yml  # CronJob 定义更新（下次调度生效）
```

### 3.4 存量数据迁移（hostPath → 桶）

```bash
# 临时迁移 Pod：只读挂两处旧目录 + mc 客户端
kubectl apply -f - <<'EOF'
apiVersion: v1
kind: Pod
metadata: { name: mc-migrate, namespace: openfindbearings }
spec:
  containers:
  - name: mc
    image: quay.io/minio/mc
    command: ["sh", "-c", "sleep infinity"]
    envFrom:
    - secretRef: { name: openfindbearings-minio-secrets }
    volumeMounts:
    - { name: old-uploads, mountPath: /srv/uploads, readOnly: true }
    - { name: old-images,  mountPath: /srv/images,  readOnly: true }
  volumes:
  - name: old-uploads
    hostPath: { path: /opt/openfindbearings/api/uploads, type: Directory }
  - name: old-images
    hostPath: { path: /opt/openfindbearings/sync/images, type: Directory }
EOF
kubectl exec -n openfindbearings mc-migrate -- sh -c '
  mc alias set local http://openfindbearings-minio:9000 "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD"
  mc mirror /srv/uploads local/media/uploads/
  mc mirror /srv/images  local/media/images/
  mc ls -r --summary local/media | tail -n 3
'
# 核对对象数与源目录文件数一致后删除迁移 Pod
kubectl delete pod -n openfindbearings mc-migrate
```

### 3.5 验证

```bash
# 1) 新上传：App 拍照上传证照 → 成功且 Admin 抽屉能看图
# 2) 存量回显：抽查旧图片 URL（/media/uploads/... 与 /media/images/...）可访问
kubectl exec -n openfindbearings deploy/openfindbearings-media -- \
  wget -qO- http://localhost/media/uploads/avatars/<某旧文件名> | head -c 8 | xxd
# 3) Sync images job 手动触发一轮，确认无写盘报错且新图入桶
```

## 4. 回滚

配置回切即可，数据双在（旧 hostPath 目录迁移后保留，确认稳定一周后再清理）：

1. API configMap `FileStorage__Provider: "LocalFile"` + 恢复 deploy.yml 的 uploads 卷与 initContainer（git revert）
2. Sync configmaps `ImageProcessing__ObjectStorage__Provider: "LocalFile"` + 恢复 images 卷与 init-chmod
3. media-server.yml 回退 alias 版（git revert）

## 5. 约定与约束

- key 规范：`uploads/avatars|merchants/logo|documents/...`（API）、`images/bearings|...`（Sync），
  均含 UTC 时间戳或 sha256，内容不可变 → 媒体层 1 年强缓存
- MinIO 单副本 + RWO PVC：与改造前 hostPath 同受"单节点"约束（当前集群单节点，成立）；
  多节点时升级 MinIO 分布式模式（4 节点纠删码）或换云 OSS，应用侧仅改 Endpoint
- 凭据纪律：root 凭据仅存 minio Secret；应用账号 ofb-app 限桶策略；仓库内一律 REPLACE_ME 模板
- 图片删除目前仅 Sync 重试清理使用（Exists/Delete），用户换头像/Logo 不删旧对象（审计留痕），
  后续可在 MinIO 配生命周期规则清理孤儿对象

## 6. 变更记录

| 版本 | 日期 | 说明 |
|---|---|---|
| v1.0.0 | 2026-09-20 | 首版：MinIO 统一对象存储改造（API v1.5.0 / Sync v17.6.0 周期），含部署、迁移、回滚与验证全流程 |
