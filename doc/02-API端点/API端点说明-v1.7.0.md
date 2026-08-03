# API 端点说明文档

**版本：** v1.7.0
**日期：** 2026-07-07
**状态：** 与代码同步

---

## 变更记录

| 版本 | 日期 | 变更说明 |
|------|------|----------|
| v1.0.0 | 2026-06-12 | 初始版本 |
| v1.1.0 | 2026-06-21 | 补充 Admin 端点权限表（17→46 端点） |
| v1.2.0 | 2026-07-06 | 全量对齐当前代码，覆盖 110 个端点，修正路由不一致、遗漏、过时项 |
| v1.3.0 | 2026-07-07 | 整合 Doc/Admin/Permission 命名统一；新增 POST /api/merchants/batch-check-verified（Sync 调用，批量查询商户认证状态） |
| v1.4.0 | 2026-07-07 | 新增 POST /api/merchant/apply（预留）、POST /api/sms/send-code（Identity）、POST /api/admin/merchants/{id}/reject；POST /api/admin/merchants/{id}/verify 新增清除爬虫数据逻辑；批量认证查询端点移至 /api/sync/merchants/batch-check-verified |
| v1.5.0 | 2026-07-07 | 新增 GET /api/admin/merchants/{id}（按 ID 查询商家详情，供 Sync 库存导入解析）|
| v1.6.0 | 2026-07-07 | 修复：BatchCheckVerifiedQueryHandler N+1（改为单 SQL 批量查询）；ApproveLicenseCommandHandler 补充爬虫数据清除和 verifiedBy；UpdateMerchantBearing/SetPriceVisibility/PutOnShelf/TakeOffShelf 四个端点新增商家所有权验证；GetMyMerchantBearings 的 pendingOnly 下推到仓储数据库层过滤 |
| v1.7.0 | 2026-07-07 | 删除虚构的 ETL 触发（6.9）和爬虫管理（6.10）端点章节（这些端点属于 Sync/Crawler 项目，不在 API 中） |

---

## 1. 概述

OpenFindBearings.Api（以下简称 API）共注册 **114** 个端点，按职责划分为 6 组。

| 组 | 路由前缀 | 端点数量 | 认证策略 |
|---|---------|---------|---------|
| 公共 | `/api` | 12 | 全部匿名 |
| 移动端 | `/api/mobile` | 4 | 全部匿名 |
| 用户 | `/api/me` | 21 | Bearer（Authenticated 策略） |
| 商家 | `/api/merchant` | 12 | Bearer（Merchant 策略，分组限流） |
| 后台管理 | `/api/admin` | 58 | Bearer（Admin 策略）+ RequirePermission |
| 同步 | `/api/sync` | 7 | Bearer（SyncClient 策略） |

---

## 2. 公共端点 `/api`（12 个）

组授权：`AllowAnonymous()`。

| 方法 | 路由 | 说明 | WithName |
|------|------|------|----------|
| GET | `/api/bearings/search` | 搜索轴承 | `SearchBearings` |
| GET | `/api/bearings/hot` | 热门轴承 | `GetHotBearings` |
| GET | `/api/bearings/{id:guid}` | 轴承详情 | `GetBearingById` |
| GET | `/api/bearings/by-code/{partNumber}` | 按型号查询轴承 | `GetBearingByPartNumber` |
| GET | `/api/bearings/{id:guid}/interchanges` | 轴承替代品列表 | `GetBearingInterchanges` |
| GET | `/api/brands` | 品牌列表 | `GetAllBrands` |
| GET | `/api/bearing-types` | 轴承类型列表 | `GetAllBearingTypes` |
| GET | `/api/merchants/search` | 搜索商家 | `SearchMerchants` |
| GET | `/api/merchants/{id:guid}` | 商家详情 | `GetMerchantById` |
| GET | `/api/merchants/{id:guid}/bearings` | 商家在售轴承 | `GetMerchantBearings` |
| POST | `/api/merchant/apply` | 商户入驻申请（预留，未在代码中实现） | `ApplyMerchant` |
| POST | `/api/sms/send-code` | 发送短信验证码（Identity 项目提供） | `SendSmsCode` |

---

## 3. 移动端点 `/api/mobile`（4 个）

组授权：`AllowAnonymous()`

| 方法 | 路由 | 说明 | WithName |
|------|------|------|----------|
| GET | `/api/mobile/home` | 移动端首页（推荐、分类、品牌） | `GetMobileHome` |
| GET | `/api/mobile/bearings/light` | 轴承轻量列表（型号+品牌+类型） | `MobileBearingLightList` |
| GET | `/api/mobile/config` | 移动端配置 | `GetMobileConfig` |
| GET | `/api/mobile/version/check` | 版本检查 | `CheckVersion` |

---

## 4. 用户端点 `/api/me`（21 个）

组授权：`RequireAuthorization("Authenticated")`

| 方法 | 路由 | 说明 |
|------|------|------|
| GET | `/api/me/profile` | 获取个人资料 |
| PUT | `/api/me/profile` | 更新个人资料 |
| GET | `/api/me/roles` | 获取当前用户角色列表 |
| GET | `/api/me/permissions` | 获取当前用户权限键列表 |

### 4.1 收藏夹

| 方法 | 路由 |
|------|------|
| GET | `/api/me/favorites/bearings` |
| POST | `/api/me/favorites/bearings/{id}` |
| DELETE | `/api/me/favorites/bearings/{id}` |
| GET | `/api/me/favorites/bearings/{id}/check` |


### 4.2 关注商家

| 方法 | 路由 |
|------|------|
| GET | `/api/me/follows/merchants` |
| POST | `/api/me/follows/merchants/{merchantId:guid}` |
| DELETE | `/api/me/follows/merchants/{merchantId:guid}` |
| GET | `/api/me/follows/merchants/{merchantId:guid}/check` |

### 4.3 浏览历史

| 方法 | 路由 |
|------|------|
| GET | `/api/me/history/bearings` |
| POST | `/api/me/history/bearings/{bearingId:guid}` |
| GET | `/api/me/history/merchants` |
| POST | `/api/me/history/merchants/{merchantId:guid}` |

### 4.4 纠错

| 方法 | 路由 |
|------|------|
| POST | `/api/me/bearings/{bearingId:guid}/corrections` |
| POST | `/api/me/merchants/{merchantId:guid}/corrections` |

---

## 5. 商家端点 `/api/merchant`（12 个）

组授权：`RequireAuthorization("Merchant")`。商家端点不通过 RequirePermission 校验，通过 ICurrentUserService 限定操作到当前商户范围。

### 5.1 店铺管理

| 方法 | 路由 | 管理员 | 员工 |
|------|------|--------|------|
| GET | `/api/merchant/profile` | 可查看 | 可查看 |
| PUT | `/api/merchant/profile` | 可操作 | 拒绝 |
| POST | `/api/merchant/license` | 可操作 | 拒绝 |

备注：`POST /api/merchant/license` 使用 `.DisableAntiforgery()`。

### 5.2 员工管理

| 方法 | 路由 | 管理员 | 员工 |
|------|------|--------|------|
| GET | `/api/merchant/staff` | 可查看 | 可查看 |
| POST | `/api/merchant/staff` | 可操作 | 拒绝 |
| DELETE | `/api/merchant/staff/{userId:guid}` | 可操作 | 拒绝 |

### 5.3 库存管理

| 方法 | 路由 | 管理员 | 员工 |
|------|------|--------|------|
| GET | `/api/merchant/bearings` | 可查看 | 可查看 |
| POST | `/api/merchant/bearings` | 可操作 | 可操作 |
| PUT | `/api/merchant/bearings/{id:guid}` | 可操作 | 可操作(仅自己添加的) |
| POST | `/api/merchant/bearings/{id:guid}/onshelf` | 可操作 | 可操作 |
| POST | `/api/merchant/bearings/{id:guid}/offshelf` | 可操作 | 可操作 |
| PUT | `/api/merchant/bearings/{id:guid}/price-visibility` | 可操作 | 可操作 |

---

## 6. 后台管理端点 `/api/admin`（58 个）

组授权：`RequireAuthorization("Admin")`。所有端点通过 `RequirePermission("bearing.xxx")` 或 `RequireRole("SuperAdmin,Admin")` 控制访问。

### 6.1 仪表盘

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/dashboard/stats` | `dashboard.view` |

### 6.2 轴承管理

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/bearings` | `bearing.view` |
| POST | `/api/admin/bearings` | `bearing.create` |
| PUT | `/api/admin/bearings/{id:guid}` | `bearing.edit` |
| DELETE | `/api/admin/bearings/{id:guid}` | `bearing.delete` |
| PUT | `/api/admin/bearings/{id:guid}/restore` | `data.restore` |

### 6.3 品牌管理

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/brands` | `bearing.view` |
| POST | `/api/admin/brands` | `bearing.create` |
| PUT | `/api/admin/brands/{id:guid}` | `bearing.edit` |
| DELETE | `/api/admin/brands/{id:guid}` | `bearing.delete` |
| PUT | `/api/admin/brands/{id:guid}/restore` | `data.restore` |
| DELETE | `/api/admin/brands/{id:guid}/hard` | `data.harddelete` |

### 6.4 轴承类型管理

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/bearing-types` | `bearing.view` |
| POST | `/api/admin/bearing-types` | `bearing.create` |
| PUT | `/api/admin/bearing-types/{id:guid}` | `bearing.edit` |
| DELETE | `/api/admin/bearing-types/{id:guid}` | `bearing.delete` |
| PUT | `/api/admin/bearing-types/{id:guid}/restore` | `data.restore` |
| DELETE | `/api/admin/bearing-types/{id:guid}/hard` | `data.harddelete` |

### 6.5 商家管理

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/merchants` | `merchant.view` |
| GET | `/api/admin/merchants/{id:guid}` | `merchant.manage` |
| POST | `/api/admin/merchants` | `merchant.manage` |
| PUT | `/api/admin/merchants/{id:guid}` | `merchant.manage` |
| DELETE | `/api/admin/merchants/{id:guid}` | `merchant.manage` |
| PUT | `/api/admin/merchants/{id:guid}/restore` | `data.restore` |
| DELETE | `/api/admin/merchants/{id:guid}/hard` | `data.harddelete` |
| POST | `/api/admin/merchants/{id:guid}/verify` | `merchant.verify` |
| POST | `/api/admin/merchants/{id:guid}/reject` | `merchant.verify` |

> `POST /api/admin/merchants/{id}/verify` 在审核通过时自动调用 `DeleteByMerchantAndSourceAsync(merchantId, DataSourceType.Crawler)` 清除该商户的爬虫 MerchantBearing 数据。

### 6.6 营业执照管理

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/licenses/pending` | `merchant.verify` |
| POST | `/api/admin/licenses/{id:guid}/approve` | `merchant.verify` |
| POST | `/api/admin/licenses/{id:guid}/reject` | `merchant.verify` |

### 6.7 纠错管理

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/corrections` | `correction.review` |
| GET | `/api/admin/corrections/pending` | `correction.review` |
| GET | `/api/admin/corrections/{id:guid}` | `correction.review` |
| POST | `/api/admin/corrections/{id:guid}/approve` | `correction.review` |
| POST | `/api/admin/corrections/{id:guid}/reject` | `correction.review` |

### 6.8 用户角色管理

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| POST | `/api/admin/users/{userId:guid}/roles` | `user.manage` |
| DELETE | `/api/admin/users/{userId:guid}/roles/{roleName}` | `user.manage` |
| GET | `/api/admin/users/{userId:guid}/roles` | `user.manage` |
| GET | `/api/admin/users/{userId:guid}/permissions` | `user.manage` |

### 6.9 角色管理

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/roles` | `role.manage` |
| GET | `/api/admin/roles/{id:guid}` | `role.manage` |
| GET | `/api/admin/roles/all` | `role.manage` |
| POST | `/api/admin/roles` | `role.manage` |
| PUT | `/api/admin/roles/{id:guid}` | `role.manage` |
| DELETE | `/api/admin/roles/{id:guid}` | `role.manage` |
| POST | `/api/admin/roles/{id:guid}/permissions` | `role.manage` |
| GET | `/api/admin/roles/{id:guid}/permissions` | `role.manage` |

### 6.10 权限管理

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/permissions` | `role.manage` |
| GET | `/api/admin/permissions/{id:guid}` | `role.manage` |
| POST | `/api/admin/permissions` | `role.manage` |
| PUT | `/api/admin/permissions/{id:guid}` | `role.manage` |
| DELETE | `/api/admin/permissions/{id:guid}` | `role.manage` |

### 6.11 审计日志

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/audit-logs` | `audit.view` |

### 6.12 系统配置

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/config` | `system.view` |
| PUT | `/api/admin/config/{key}` | `system.manage` |
| POST | `/api/admin/cache/refresh-rate-limit` | `system.manage` |
| GET | `/api/admin/config/price` | `system.view` |

---

## 7. 同步端点 `/api/sync`（7 个）

组授权：`RequireAuthorization("SyncClient")`。用于 FindBearings.Sync 项目的 L 阶段数据加载。

| 方法 | 路由 | 说明 | WithName |
|------|------|------|----------|
| POST | `/api/sync/brands/batch` | 批量同步品牌 | `SyncBrands` |
| POST | `/api/sync/bearingtypes/batch` | 批量同步轴承类型 | `SyncBearingTypes` |
| POST | `/api/sync/bearings/batch` | 批量同步轴承 | `BatchCreateBearings` |
| POST | `/api/sync/merchants/batch` | 批量同步商家 | `BatchCreateMerchants` |
| POST | `/api/sync/merchantbearings/batch` | 批量同步关联 | `BatchCreateMerchantBearings` |
| POST | `/api/sync/interchanges/batch` | 批量同步替代品 | `BatchCreateInterchanges` |
| POST | `/api/sync/merchants/batch-check-verified` | 批量查询商户是否已认证 | `BatchCheckMerchantsVerified` |

---

## 8. 端口配置

| 环境 | HTTP | HTTPS |
|------|------|-------|
| 开发 | 5183 | 7183 |
| 生产 | 8080 | — |

---

## 9. 健康检查端点

| 端点 | 说明 |
|------|------|
| `/health` | 详细 JSON 健康检查 |
| `/healthz` | 纯文本健康检查 |
| `/health/live` | K8s 存活探针 |
| `/health/ready` | K8s 就绪探针 |
