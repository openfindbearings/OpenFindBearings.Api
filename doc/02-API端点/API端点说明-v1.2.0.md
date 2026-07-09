# API 端点说明文档

**版本：** v1.3.0
**日期：** 2026-07-06
**状态：** 与代码同步

---

## 变更记录

| 版本 | 日期 | 变更说明 |
|------|------|----------|
| v1.0.0 | 2026-06-12 | 初始版本 |
| v1.1.0 | 2026-06-21 | 补充 Admin 端点权限表（17→46 端点） |
| v1.2.0 | 2026-07-06 | 全量对齐当前代码，覆盖 110 个端点，修正路由不一致、遗漏、过时项 |
| v1.3.0 | 2026-07-06 | 新增 POST /api/merchants/batch-check-verified（Sync 调用，批量查询商户认证状态） |

---

## 1. 概述

OpenFindBearings.Api 对外暴露的 API 端点按功能分为 6 组：

| 组 | 路由前缀 | 授权策略 | 端点数 |
|----|----------|----------|--------|
| 公开 | `/api` | 匿名 | 11 |
| 移动 | `/api/mobile` | 匿名 | 4 |
| 用户 | `/api/me` | Authenticated | 21 |
| 商家 | `/api/merchant` | Merchant | 12 |
| 管理 | `/api/admin` | Admin + Permission | 56 |
| 同步 | `/api/sync` | SyncClient | 6 |

---

## 2. 公开端点 `/api`（11 个）

组授权：`AllowAnonymous()`

| 方法 | 路由 | 说明 | WithName |
|------|------|------|----------|
| GET | `/api/login-methods` | 获取可用的登录方式列表 | `GetLoginMethods` |
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

### 4.1 用户基础

| 方法 | 路由 | 说明 |
|------|------|------|
| GET | `/api/me/profile` | 获取当前用户资料 |
| PUT | `/api/me/profile` | 更新当前用户资料 |
| GET | `/api/me/permissions` | 获取当前用户权限列表 |
| GET | `/api/me/roles` | 获取当前用户角色列表 |

### 4.2 轴承收藏

| 方法 | 路由 | 说明 |
|------|------|------|
| GET | `/api/me/favorites/bearings` | 收藏列表（分页） |
| POST | `/api/me/favorites/bearings/{bearingId:guid}` | 添加收藏 |
| DELETE | `/api/me/favorites/bearings/{bearingId:guid}` | 取消收藏 |
| GET | `/api/me/favorites/bearings/{bearingId:guid}/check` | 检查是否已收藏 |

### 4.3 商家关注

| 方法 | 路由 | 说明 |
|------|------|------|
| GET | `/api/me/follows/merchants` | 关注列表（分页） |
| POST | `/api/me/follows/merchants/{merchantId:guid}` | 添加关注 |
| DELETE | `/api/me/follows/merchants/{merchantId:guid}` | 取消关注 |
| GET | `/api/me/follows/merchants/{merchantId:guid}/check` | 检查是否已关注 |

### 4.4 浏览历史

| 方法 | 路由 | 说明 |
|------|------|------|
| GET | `/api/me/history/bearings` | 轴承浏览历史 |
| POST | `/api/me/history/bearings/{bearingId:guid}` | 记录轴承浏览 |
| GET | `/api/me/history/merchants` | 商家浏览历史 |
| POST | `/api/me/history/merchants/{merchantId:guid}` | 记录商家浏览 |
| DELETE | `/api/me/history/clear` | 清空浏览历史 |

### 4.5 纠错功能

| 方法 | 路由 | 说明 |
|------|------|------|
| GET | `/api/me/corrections` | 纠错记录列表 |
| GET | `/api/me/corrections/{id:guid}` | 纠错详情 |
| POST | `/api/me/bearings/{bearingId:guid}/corrections` | 提交轴承纠错 |
| POST | `/api/me/merchants/{merchantId:guid}/corrections` | 提交商家纠错 |

---

## 5. 商家端点 `/api/merchant`（12 个）

组授权：`RequireAuthorization("Merchant")`。商家端点不通过 RequirePermission 校验，通过 ICurrentUserService 限定操作到当前商户范围。

### 5.1 基础管理

| 方法 | 路由 | 说明 | 管理员 | 员工 |
|------|------|------|--------|------|
| GET | `/api/merchant/profile` | 获取店铺信息 | 可查看 | 可查看 |
| PUT | `/api/merchant/profile` | 更新店铺信息 | 可操作 | 拒绝 |
| POST | `/api/merchant/license` | 上传营业执照 | 可操作 | 拒绝 |

备注：`POST /api/merchant/license` 使用 `.DisableAntiforgery()`。

### 5.2 员工管理

| 方法 | 路由 | 说明 | 管理员 | 员工 |
|------|------|------|--------|------|
| GET | `/api/merchant/staff` | 获取员工列表 | 可查看 | 可查看 |
| POST | `/api/merchant/staff` | 添加员工 | 可操作 | 拒绝 |
| DELETE | `/api/merchant/staff/{userId:guid}` | 移除员工 | 可操作 | 拒绝 |

### 5.3 产品管理

| 方法 | 路由 | 说明 | 管理员 | 员工 |
|------|------|------|--------|------|
| GET | `/api/merchant/bearings` | 获取自家轴承列表 | 可查看 | 可查看 |
| POST | `/api/merchant/bearings` | 添加轴承到店铺 | 可操作 | 可操作 |
| PUT | `/api/merchant/bearings/{id:guid}` | 更新轴承信息 | 可操作 | 可操作 |
| POST | `/api/merchant/bearings/{id:guid}/onshelf` | 上架轴承 | 可操作 | 可操作 |
| POST | `/api/merchant/bearings/{id:guid}/offshelf` | 下架轴承 | 可操作 | 可操作 |
| PUT | `/api/merchant/bearings/{id:guid}/price-visibility` | 设置价格可见性 | 可操作 | 可操作 |

---

## 6. 管理端点 `/api/admin`（56 个）

组授权：`RequireAuthorization("Admin")`。各端点通过 `RequirePermission("...")` 校验。

### 6.1 仪表盘

| 方法 | 路由 | 权限 |
|------|------|------|
| GET | `/api/admin/dashboard/stats` | `dashboard.view` |
| GET | `/api/admin/audit-logs` | `audit.view` |

### 6.2 品牌管理

| 方法 | 路由 | 权限 |
|------|------|------|
| GET | `/api/admin/brands` | `bearing.view` |
| POST | `/api/admin/brands` | `bearing.create` |
| PUT | `/api/admin/brands/{id:guid}` | `bearing.edit` |
| DELETE | `/api/admin/brands/{id:guid}` | `bearing.delete` |
| PUT | `/api/admin/brands/{id:guid}/restore` | `data.restore` |
| DELETE | `/api/admin/brands/{id:guid}/hard` | `data.harddelete` |

### 6.3 轴承类型管理

| 方法 | 路由 | 权限 |
|------|------|------|
| GET | `/api/admin/bearing-types` | `bearing.view` |
| POST | `/api/admin/bearing-types` | `bearing.create` |
| PUT | `/api/admin/bearing-types/{id:guid}` | `bearing.edit` |
| DELETE | `/api/admin/bearing-types/{id:guid}` | `bearing.delete` |
| PUT | `/api/admin/bearing-types/{id:guid}/restore` | `data.restore` |
| DELETE | `/api/admin/bearing-types/{id:guid}/hard` | `data.harddelete` |

### 6.4 轴承管理

| 方法 | 路由 | 权限 |
|------|------|------|
| GET | `/api/admin/bearings` | `bearing.view` |
| POST | `/api/admin/bearings` | `bearing.create` |
| PUT | `/api/admin/bearings/{id:guid}` | `bearing.edit` |
| DELETE | `/api/admin/bearings/{id:guid}` | `bearing.delete` |
| PUT | `/api/admin/bearings/{id:guid}/restore` | `data.restore` |
| DELETE | `/api/admin/bearings/{id:guid}/hard` | `data.harddelete` |

### 6.5 商家管理

| 方法 | 路由 | 权限 |
|------|------|------|
| GET | `/api/admin/merchants` | `merchant.view` |
| POST | `/api/admin/merchants` | `merchant.manage` |
| PUT | `/api/admin/merchants/{id:guid}` | `merchant.manage` |
| DELETE | `/api/admin/merchants/{id:guid}` | `merchant.manage` |
| PUT | `/api/admin/merchants/{id:guid}/restore` | `data.restore` |
| DELETE | `/api/admin/merchants/{id:guid}/hard` | `data.harddelete` |
| POST | `/api/admin/merchants/{id:guid}/verify` | `merchant.verify` |

### 6.6 营业执照审核

| 方法 | 路由 | 权限 |
|------|------|------|
| GET | `/api/admin/licenses/pending` | `merchant.verify` |
| POST | `/api/admin/licenses/{id:guid}/approve` | `merchant.verify` |
| POST | `/api/admin/licenses/{id:guid}/reject` | `merchant.verify` |

### 6.7 纠错审核

| 方法 | 路由 | 权限 |
|------|------|------|
| GET | `/api/admin/corrections` | `correction.review` |
| GET | `/api/admin/corrections/pending` | `correction.review` |
| GET | `/api/admin/corrections/{id:guid}` | `correction.review` |
| POST | `/api/admin/corrections/{id:guid}/approve` | `correction.review` |
| POST | `/api/admin/corrections/{id:guid}/reject` | `correction.review` |

### 6.8 角色管理

| 方法 | 路由 | 权限 |
|------|------|------|
| GET | `/api/admin/roles` | `role.manage` |
| GET | `/api/admin/roles/all` | `role.manage` |
| GET | `/api/admin/roles/{id:guid}` | `role.manage` |
| POST | `/api/admin/roles` | `role.manage` |
| PUT | `/api/admin/roles/{id:guid}` | `role.manage` |
| DELETE | `/api/admin/roles/{id:guid}` | `role.manage` |
| POST | `/api/admin/roles/{id:guid}/permissions` | `role.manage` |
| GET | `/api/admin/roles/{id:guid}/permissions` | `role.manage` |

### 6.9 权限管理

| 方法 | 路由 | 权限 |
|------|------|------|
| GET | `/api/admin/permissions` | `role.manage` |
| GET | `/api/admin/permissions/{id:guid}` | `role.manage` |
| POST | `/api/admin/permissions` | `role.manage` |
| PUT | `/api/admin/permissions/{id:guid}` | `role.manage` |
| DELETE | `/api/admin/permissions/{id:guid}` | `role.manage` |

### 6.10 用户角色关联

| 方法 | 路由 | 权限 |
|------|------|------|
| GET | `/api/admin/users/{userId:guid}/roles` | `user.manage` |
| POST | `/api/admin/users/{userId:guid}/roles` | `user.manage` |
| DELETE | `/api/admin/users/{userId:guid}/roles/{roleName}` | `user.manage` |
| GET | `/api/admin/users/{userId:guid}/permissions` | `user.manage` |

### 6.11 系统配置

| 方法 | 路由 | 权限 |
|------|------|------|
| GET | `/api/admin/config` | `system.view` |
| GET | `/api/admin/config/price` | `system.view` |
| PUT | `/api/admin/config/{key}` | `system.manage` |
| POST | `/api/admin/cache/refresh-rate-limit` | 无（内部维护接口） |

---

## 7. 同步端点 `/api/sync`（6 个）

组授权：`RequireAuthorization("SyncClient")`。用于 FindBearings.Sync 项目的 L 阶段数据加载。

| 方法 | 路由 | 说明 | WithName |
|------|------|------|----------|
| POST | `/api/sync/brands/batch` | 批量同步品牌 | `SyncBrands` |
| POST | `/api/sync/bearingtypes/batch` | 批量同步轴承类型 | `SyncBearingTypes` |
| POST | `/api/sync/bearings/batch` | 批量同步轴承 | `BatchCreateBearings` |
| POST | `/api/sync/merchants/batch` | 批量同步商家 | `BatchCreateMerchants` |
| POST | `/api/sync/merchantbearings/batch` | 批量同步关联 | `BatchCreateMerchantBearings` |
| POST | `/api/sync/interchanges/batch` | 批量同步替代品 | `BatchCreateInterchanges` |

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
