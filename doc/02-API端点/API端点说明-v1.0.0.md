## OpenFindBearings.Api API 端点说明 v1.0.0

**文档信息**

| 项目 | 内容 |
|------|------|
| 文档名称 | OpenFindBearings.Api API 端点说明 |
| 版本 | v1.0.0 |
| 最后更新 | 2026-06-06 |

---

### 1. 端点文件映射

| 文件 | 路由组 | 认证策略 |
|------|--------|------|
| `Endpoints/PublicEndpoints.cs` | `/api` | 匿名 |
| `Endpoints/MobileEndpoints.cs` | `/api/mobile` | 匿名 |
| `Endpoints/UserEndpoints.cs` | `/api/me` | Authenticated |
| `Endpoints/MerchantEndpoints.cs` | `/api/merchant` | Merchant 策略 |
| `Endpoints/AdminEndpoints.cs` | `/api/admin` | Admin 策略 |
| `Endpoints/SyncEndpoints.cs` | `/api/sync` | SyncClient 策略 |

---

### 2. 公开端点 (`/api`) — PublicEndpoints.cs

所有匿名访问。

| 方法 | 路由 | 说明 |
|------|------|------|
| GET | `/api/bearings/search` | 轴承搜索（关键词、品牌、类型过滤） |
| GET | `/api/bearings/{id}` | 获取轴承详情 |
| GET | `/api/brands` | 品牌列表 |
| GET | `/api/brands/{id}` | 品牌详情 |
| GET | `/api/bearing-types` | 轴承类型列表 |
| GET | `/api/merchants/search` | 商家搜索 |
| GET | `/api/merchants/{id}` | 商家详情 |
| GET | `/api/merchants/{id}/bearings` | 商家在售轴承列表 |
| GET | `/api/interchanges/{bearingId}` | 轴承替代品列表 |

---

### 3. 移动端点 (`/api/mobile`) — MobileEndpoints.cs

匿名访问，响应格式适配移动端。

| 方法 | 路由 | 说明 |
|------|------|------|
| GET | `/api/mobile/home` | 首页数据聚合 |
| GET | `/api/mobile/categories` | 分类导航数据 |

---

### 4. 用户端点 (`/api/me`) — UserEndpoints.cs

需要 JWT 认证。

| 方法 | 路由 | 说明 |
|------|------|------|
| GET | `/api/me/profile` | 获取个人资料 |
| PUT | `/api/me/profile` | 更新个人资料 |
| GET | `/api/me/favorites` | 收藏轴承列表 |
| POST | `/api/me/favorites/{bearingId}` | 收藏轴承 |
| DELETE | `/api/me/favorites/{bearingId}` | 取消收藏 |
| GET | `/api/me/follows` | 关注商家列表 |
| POST | `/api/me/follows/{merchantId}` | 关注商家 |
| DELETE | `/api/me/follows/{merchantId}` | 取消关注 |
| GET | `/api/me/history/bearings` | 浏览记录 |
| GET | `/api/me/history/merchants` | 商家浏览记录 |
| GET | `/api/me/preferences` | 用户偏好设置 |
| PUT | `/api/me/preferences` | 更新偏好设置 |

---

### 5. 商家端点 (`/api/merchant`) — MerchantEndpoints.cs

需要 Merchant 角色认证。

| 方法 | 路由 | 说明 |
|------|------|------|
| GET | `/api/merchant/dashboard` | 商家仪表盘 |
| PUT | `/api/merchant/profile` | 更新商家资料 |
| POST | `/api/merchant/bearings` | 上架轴承 |
| PUT | `/api/merchant/bearings/{id}` | 编辑轴承价格库存 |
| DELETE | `/api/merchant/bearings/{id}` | 下架轴承 |
| GET | `/api/merchant/bearings` | 商家在售轴承列表 |
| GET | `/api/merchant/orders` | 最近询价/订单 |

---

### 6. 管理端点 (`/api/admin`) — AdminEndpoints.cs

需要 Admin 角色认证。

| 方法 | 路由 | 说明 |
|------|------|------|
| GET | `/api/admin/dashboard` | 管理仪表盘 |
| GET | `/api/admin/users` | 用户列表（分页） |
| PUT | `/api/admin/users/{id}` | 编辑用户 |
| PATCH | `/api/admin/users/{id}/status` | 启用/禁用用户 |
| GET | `/api/admin/bearings` | 轴承管理列表 |
| POST | `/api/admin/bearings` | 手动创建轴承 |
| PUT | `/api/admin/bearings/{id}` | 编辑轴承 |
| DELETE | `/api/admin/bearings/{id}` | 删除轴承 |
| GET | `/api/admin/brands` | 品牌管理列表 |
| POST | `/api/admin/brands` | 创建品牌 |
| PUT | `/api/admin/brands/{id}` | 编辑品牌 |
| DELETE | `/api/admin/brands/{id}` | 删除品牌 |
| GET | `/api/admin/merchants` | 商家审核列表 |
| PATCH | `/api/admin/merchants/{id}/approve` | 审核通过商家 |
| PATCH | `/api/admin/merchants/{id}/reject` | 驳回商家 |
| GET | `/api/admin/corrections` | 纠错请求列表 |
| PATCH | `/api/admin/corrections/{id}/resolve` | 处理纠错请求 |
| GET | `/api/admin/audit-logs` | 审计日志 |
| GET | `/api/admin/system-configs` | 系统配置 |
| PUT | `/api/admin/system-configs/{key}` | 更新系统配置 |

---

### 7. 同步端点 (`/api/sync`) — SyncEndpoints.cs

需要 SyncClient 策略（`scope=api:sync` JWT claim），由 FindBearings.Sync 项目的 LoadService 调用。

详见 [同步接口设计文档](../03-TongBuJiZhi/同步接口设计-v1.0.0.md)。

| 方法 | 路由 | 说明 |
|------|------|------|
| POST | `/api/sync/brands/batch` | 批量同步品牌 |
| POST | `/api/sync/bearingtypes/batch` | 批量同步轴承类型 |
| POST | `/api/sync/bearings/batch` | 批量同步轴承 |
| POST | `/api/sync/merchants/batch` | 批量同步商家 |
| POST | `/api/sync/merchantbearings/batch` | 批量同步商家-轴承关联 |
| POST | `/api/sync/interchanges/batch` | 批量同步替代品关系 |
| GET | `/api/sync/tasks/{taskId}` | 查询异步任务状态 |

---

### 8. 健康检查端点

注册于 `EndpointRouteBuilderExtensions.cs`。

| 路由 | 说明 | 用途 |
|------|------|------|
| `/health` | 详细 JSON 健康报告 | 人工排障 |
| `/healthz` | 简洁状态 (Unhealthy=503) | K3s 探针 |
| `/live` | 存活探针 | K3s livenessProbe |
| `/ready` | 就绪探针（数据库+OpenIddict 验证） | K3s readinessProbe |

---

### 9. 版本历史

| 版本 | 日期 | 变更说明 |
|------|------|------|
| v1.0.0 | 2026-06-06 | 初始版本，覆盖全部 7 组端点 |
