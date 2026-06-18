## OpenFindBearings.Api API 端点说明 v1.1.0

**文档信息**

| 项目 | 内容 |
|------|------|
| 文档名称 | OpenFindBearings.Api API 端点说明 |
| 版本 | v1.1.0 |
| 最后更新 | 2026-06-15 |

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

需要 Admin 角色认证 + `RequirePermission` 具体权限门禁。

| 方法 | 路由 | 说明 | 所需权限 |
|------|------|------|----------|
| GET | `/api/admin/dashboard/stats` | 仪表盘统计（轴承/品牌/商家/待审核数） | dashboard.view |
| GET | `/api/admin/audit-logs` | 审计日志列表 | audit.view |
| POST | `/api/admin/bearings` | 创建轴承 | bearing.create |
| PUT | `/api/admin/bearings/{id}` | 编辑轴承 | bearing.edit |
| DELETE | `/api/admin/bearings/{id}` | 软删除轴承 | bearing.delete |
| PUT | `/api/admin/bearings/{id}/restore` | 恢复轴承 | data.restore |
| DELETE | `/api/admin/bearings/{id}/hard` | 彻底删除轴承 | data.harddelete |
| POST | `/api/admin/brands` | 创建品牌 | bearing.create |
| PUT | `/api/admin/brands/{id}` | 编辑品牌 | bearing.edit |
| DELETE | `/api/admin/brands/{id}` | 软删除品牌 | bearing.delete |
| PUT | `/api/admin/brands/{id}/restore` | 恢复品牌 | data.restore |
| DELETE | `/api/admin/brands/{id}/hard` | 彻底删除品牌 | data.harddelete |
| POST | `/api/admin/bearing-types` | 创建轴承类型 | bearing.create |
| PUT | `/api/admin/bearing-types/{id}` | 编辑轴承类型 | bearing.edit |
| DELETE | `/api/admin/bearing-types/{id}` | 软删除轴承类型 | bearing.delete |
| PUT | `/api/admin/bearing-types/{id}/restore` | 恢复轴承类型 | data.restore |
| DELETE | `/api/admin/bearing-types/{id}/hard` | 彻底删除轴承类型 | data.harddelete |
| POST | `/api/admin/merchants` | 创建商家 | merchant.manage |
| PUT | `/api/admin/merchants/{id}` | 编辑商家 | merchant.manage |
| DELETE | `/api/admin/merchants/{id}` | 软删除商家 | merchant.manage |
| PUT | `/api/admin/merchants/{id}/restore` | 恢复商家 | data.restore |
| DELETE | `/api/admin/merchants/{id}/hard` | 彻底删除商家 | data.harddelete |
| POST | `/api/admin/merchants/{id}/verify` | 认证商家 | merchant.verify |
| GET | `/api/admin/corrections` | 纠错请求列表 | correction.review |
| GET | `/api/admin/corrections/pending` | 待审核纠错列表 | correction.review |
| POST | `/api/admin/corrections/{id}/approve` | 审批通过纠错 | correction.review |
| POST | `/api/admin/corrections/{id}/reject` | 审批拒绝纠错 | correction.review |
| GET | `/api/admin/corrections/bearing/{bearingId}` | 按轴承查纠错 | correction.review |
| GET | `/api/admin/licenses` | 营业执照列表 | merchant.verify |
| POST | `/api/admin/licenses/{id}/approve` | 通过营业执照 | merchant.verify |
| POST | `/api/admin/licenses/{id}/reject` | 拒绝营业执照 | merchant.verify |
| GET | `/api/admin/merchants/pending-verify` | 待认证商家列表 | merchant.verify |
| GET | `/api/admin/roles` | 角色列表（分页） | role.manage |
| GET | `/api/admin/roles/all` | 全部角色 | role.manage |
| POST | `/api/admin/roles` | 创建角色 | role.manage |
| GET | `/api/admin/roles/{id}` | 角色详情 | role.manage |
| PUT | `/api/admin/roles/{id}` | 编辑角色 | role.manage |
| DELETE | `/api/admin/roles/{id}` | 删除角色 | role.manage |
| GET | `/api/admin/roles/{id}/permissions` | 角色权限列表 | role.manage |
| PUT | `/api/admin/roles/{id}/permissions` | 更新角色权限 | role.manage |
| GET | `/api/admin/permissions` | 权限列表（分页） | role.manage |
| GET | `/api/admin/permissions/all` | 全部权限 | role.manage |
| GET | `/api/admin/permissions/{id}` | 权限详情 | role.manage |
| GET | `/api/admin/users` | 用户列表 | user.manage |
| POST | `/api/admin/users` | 创建用户 | user.manage |
| GET | `/api/admin/users/{id}` | 用户详情 | user.manage |
| PUT | `/api/admin/users/{id}` | 编辑用户 | user.manage |
| DELETE | `/api/admin/users/{id}` | 删除用户 | user.manage |
| GET | `/api/admin/config` | 系统配置列表 | system.view |
| PUT | `/api/admin/config/{key}` | 更新系统配置 | system.manage |
| GET | `/api/admin/cache/refresh-rate-limit` | 刷新限流缓存 | system.manage |

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
| v1.1.0 | 2026-06-15 | 更新 Admin 端点表为实际实现（46 个端点，标注权限门禁）；新增 dashboard/stats 仪表盘统计端点 |
