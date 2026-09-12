# API 端点说明文档

**版本：** v1.14.0
**日期：** 2026-09-12
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
| v1.8.0 | 2026-07-09 | 移除 POST /api/sync/merchants/batch-check-verified 端点（不再需要已认证商户预检）；移除 VerifyMerchantCommandHandler/ApproveLicenseCommandHandler 中 DeleteByMerchantAndSourceAsync 调用（审核不再清除爬虫数据）；移除 IMerchantBearingRepository.DeleteByMerchantAndSourceAsync 和 IMerchantRepository.GetVerifiedNamesAsync；GET /api/merchants/{id}/bearings 新增 dataSource（Manual/Crawler）和 onlyOnSale（bool）查询参数；端点总数 114 → 113，同步端点 7 → 6 |
| v1.9.0 | 2026-08-20 | GET /api/admin/merchants 新增 excludeCrawler（bool）查询参数，排除爬虫来源商家，仅显示入驻申请商家；SearchMerchantsQuery/MerchantSearchParams 同步支持 ExcludeCrawler |
| v1.10.0 | 2026-08-29 | 新增 GET /api/bearings/{id:guid}/merchants（轴承在售商家反向查询，支持 onlyOnSale 与分页参数），端点总数 113 → 114，公共端点组 12 → 13 |
| v1.11.0 | 2026-08-29 | 新增 GET /api/config/reliability（可信度阈值，供 Sync 运行时从 SystemConfigs 拉取，替代其 appsettings 默认值）；系统配置章节补充该内部端点 |
| v1.12.0 | 2026-09-08 | GET /api/bearings/search 移除"至少一个搜索条件"强校验（原抛 InvalidOperationException 被映射为 400），无条件时返回空/全量分页，与商家搜索一致；限流中间件新增内部服务间调用豁免（X-Internal-Token，见《系统配置与限流设计 v1.1.0》） |
| v1.13.0 | 2026-09-11 | 代码审查对齐：① 公共端点组补充 GET /api/login-methods（13→14）；② 用户端点组补充 POST /api/me/avatar、GET /api/me/corrections、GET /api/me/corrections/{id}（21→24）；③ 管理端点 6.2 轴承管理补充 DELETE /api/admin/bearings/{id:guid}/hard；④ 总端点数 114→118 |
| v1.14.0 | 2026-09-12 | 商户入驻整体落地：① 新增"商户入驻"端点组（6 个）：POST /api/merchant/apply（预留转正式实现）、GET /api/merchant/application、POST /api/merchant/nominate、POST /api/merchant/nominate/{code}/accept、GET /api/merchant/claimable、GET /api/merchant/nominations/pending；② 商家端点组 12→16：新增 POST /api/merchant/members/{userId:guid}/suspend、POST /api/merchant/members/{userId:guid}/activate、PUT /api/merchant/members/{userId:guid}/role、POST /api/merchant/inventory/import（Excel 批量导入，仅商户管理员），并补成员管理小节；③ Admin 商家管理 58→60：新增 POST /api/admin/merchants/{id}/approve（审核通过入驻申请，Pending→Active，与 verify 认证分离）、POST /api/admin/merchants/{id}/members（平台指定成员兜底，复用 merchant.verify）；④ 总端点数 118→129，引导表格分组同步更新 |

---

## 1. 概述

OpenFindBearings.Api（以下简称 API）共注册 **129** 个端点，按职责划分为 7 组。另提供内部配置端点 `/api/config/reliability` 供 Sync 拉取可信度阈值（不计入 7 组统计）。

| 组 | 路由前缀 | 端点数量 | 认证策略 |
|---|---------|---------|---------|
| 公共 | `/api` | 13 | 全部匿名 |
| 移动端 | `/api/mobile` | 4 | 全部匿名 |
| 用户 | `/api/me` | 24 | Bearer（Authenticated 策略） |
| 入驻 | `/api/merchant` | 6 | Bearer（任意已登录用户） |
| 商家 | `/api/merchant` | 16 | Bearer（Merchant 策略，分组限流） |
| 后台管理 | `/api/admin` | 60 | Bearer（Admin 策略）+ RequirePermission |
| 同步 | `/api/sync` | 6 | Bearer（SyncClient 策略） |

---

## 2. 公共端点 `/api`（13 个）

组授权：`AllowAnonymous()`。

| 方法 | 路由 | 说明 | WithName |
|------|------|------|----------|
| GET | `/api/bearings/search` | 搜索轴承（无条件时返回空/全量分页，不再 400） | `SearchBearings` |
| GET | `/api/bearings/hot` | 热门轴承 | `GetHotBearings` |
| GET | `/api/bearings/{id:guid}` | 轴承详情 | `GetBearingById` |
| GET | `/api/bearings/by-code/{partNumber}` | 按型号查询轴承 | `GetBearingByPartNumber` |
| GET | `/api/bearings/{id:guid}/interchanges` | 轴承替代品列表 | `GetBearingInterchanges` |
| GET | `/api/bearings/{id:guid}/merchants` | 轴承在售商家（反向查询，支持 onlyOnSale 与分页） | `GetMerchantsByBearing` |
| GET | `/api/brands` | 品牌列表 | `GetAllBrands` |
| GET | `/api/bearing-types` | 轴承类型列表 | `GetAllBearingTypes` |
| GET | `/api/merchants/search` | 搜索商家 | `SearchMerchants` |
| GET | `/api/merchants/{id:guid}` | 商家详情 | `GetMerchantById` |
| GET | `/api/merchants/{id:guid}/bearings` | 商家在售轴承（支持 dataSource 和 onlyOnSale 参数） | `GetMerchantBearings` |
| POST | `/api/sms/send-code` | 发送短信验证码（Identity 项目提供） | `SendSmsCode` |
| GET | `/api/login-methods` | 登录方式信息（返回 token 端点、注销端点、注册端点、支持的 grant 类型） | `GetLoginMethods` |

> v1.14.0 说明：`POST /api/merchant/apply` 已由"预留"转为正式实现，归入第 4 章"商户入驻"端点组（需登录），不再列于公共组；公共组 14 → 13。

> `GET /api/merchants/{id}/bearings` 支持以下查询参数：
> - `dataSource`：可选值 `Manual`（默认）、`Crawler`。未传时按 Manual 优先展示
> - `onlyOnSale`：可选值 `true`、`false`（默认）。设为 `true` 时仅返回在售记录

> `GET /api/bearings/{id:guid}/merchants` 支持以下查询参数：
> - `onlyOnSale`：可选值 `true`、`false`（默认 `true`）。设为 `true` 时仅返回在售商家
> - `page` / `pageSize`：分页参数（默认 page=1、pageSize=20）

---

## 3. 移动端点 `/api/mobile`（4 个）

组授权：`AllowAnonymous()`

| 方法 | 路由 | 说明 | WithName |
|------|------|------|----------|
| GET | `/api/mobile/home` | 移动端首页（推荐、分类、品牌） | `GetMobileHome` |
| GET | `/api/mobile/bearings/light` | 轴承轻量列表（型号+品牌+类型） | `MobileBearingLightList` |
| GET | `/api/mobile/config` | 移动端配置（含站点名称/备案号/客服联系方式） | `GetMobileConfig` |
| GET | `/api/mobile/version/check` | 版本检查 | `CheckVersion` |

---

## 4. 入驻端点 `/api/merchant`（6 个）

组授权：`RequireAuthorization()`（任意已登录用户即可，非 Merchant 策略）。

| 方法 | 路由 | 说明 | WithName |
|------|------|------|----------|
| POST | `/api/merchant/apply` | 提交入驻申请（mode=self 自助新建 / mode=claim 认领爬虫商家；申请人成为该商户管理员） | `ApplyMerchant` |
| GET | `/api/merchant/application` | 查询当前用户在各商户的入驻进度（待审核/已生效/已拒绝） | `GetMerchantApplication` |
| POST | `/api/merchant/nominate` | 提名他人为管理员（模式 B，建 Draft 商户 + Nomination 邀请；发起人默认入伙为员工） | `NominateMerchant` |
| POST | `/api/merchant/nominate/{code}/accept` | 被提名人接受提名并补全资料（Draft → Pending；手机号取自 JWT 防冒领） | `AcceptNomination` |
| GET | `/api/merchant/claimable` | 认领搜索可认领的爬虫来源商家（未被认领），支持 keyword/page/pageSize | `GetClaimableMerchants` |
| GET | `/api/merchant/nominations/pending` | 按当前登录用户手机号匹配待我接受的管理员提名邀请 | `GetPendingNominations` |

---

## 5. 用户端点 `/api/me`（24 个）

组授权：`RequireAuthorization("Authenticated")`

| 方法 | 路由 | 说明 |
|------|------|------|
| GET | `/api/me/profile` | 获取个人资料 |
| PUT | `/api/me/profile` | 更新个人资料 |
| POST | `/api/me/avatar` | 上传头像（jpg/png/webp，≤2MB） |
| GET | `/api/me/roles` | 获取当前用户角色列表 |
| GET | `/api/me/permissions` | 获取当前用户权限键列表 |

### 5.1 收藏夹

| 方法 | 路由 |
|------|------|
| GET | `/api/me/favorites/bearings` |
| POST | `/api/me/favorites/bearings/{id}` |
| DELETE | `/api/me/favorites/bearings/{id}` |
| GET | `/api/me/favorites/bearings/{id}/check` |

### 5.2 关注商家

| 方法 | 路由 |
|------|------|
| GET | `/api/me/follows/merchants` |
| POST | `/api/me/follows/merchants/{merchantId:guid}` |
| DELETE | `/api/me/follows/merchants/{merchantId:guid}` |
| GET | `/api/me/follows/merchants/{merchantId:guid}/check` |

### 5.3 浏览历史

| 方法 | 路由 |
|------|------|
| GET | `/api/me/history/bearings` |
| POST | `/api/me/history/bearings/{bearingId:guid}` |
| GET | `/api/me/history/merchants` |
| POST | `/api/me/history/merchants/{merchantId:guid}` |
| DELETE | `/api/me/history/bearings/{id:guid}` |
| DELETE | `/api/me/history/merchants/{id:guid}` |
| DELETE | `/api/me/history/clear` |

### 5.4 纠错

| 方法 | 路由 |
|------|------|
| GET | `/api/me/corrections` | 获取当前用户的纠错列表 |
| GET | `/api/me/corrections/{id:guid}` | 获取纠错详情 |
| POST | `/api/me/bearings/{bearingId:guid}/corrections` |
| POST | `/api/me/merchants/{merchantId:guid}/corrections` |

---

## 6. 商家端点 `/api/merchant`（16 个）

组授权：`RequireAuthorization("Merchant")`。商家端点不通过 RequirePermission 校验，通过 ICurrentUserService 限定操作到当前商户范围（X-Merchant-Id 或首个在职成员商户）。

### 6.1 店铺管理

| 方法 | 路由 | 管理员 | 员工 |
|------|------|--------|------|
| GET | `/api/merchant/profile` | 可查看 | 可查看 |
| PUT | `/api/merchant/profile` | 可操作 | 拒绝 |
| POST | `/api/merchant/license` | 可操作 | 拒绝 |

备注：`POST /api/merchant/license` 使用 `.DisableAntiforgery()`。

### 6.2 成员管理

| 方法 | 路由 | 管理员 | 员工 |
|------|------|--------|------|
| GET | `/api/merchant/staff` | 可查看 | 可查看 |
| POST | `/api/merchant/staff` | 可操作 | 拒绝 |
| DELETE | `/api/merchant/staff/{userId:guid}` | 可操作 | 拒绝 |
| POST | `/api/merchant/members/{userId:guid}/suspend` | 可操作 | 拒绝 |
| POST | `/api/merchant/members/{userId:guid}/activate` | 可操作 | 拒绝 |
| PUT | `/api/merchant/members/{userId:guid}/role` | 可操作 | 拒绝 |

> v1.14.0 说明：suspend（停用成员，保留关系但无操作权限）/ activate（恢复成员）/ role（变更 MerchantAdmin/MerchantStaff 角色）为新增端点，管理端操作均以 `permissionService.IsMerchantAdmin()`（查成员表 + 当前商户上下文）为准；移除最后一个 Active 管理员被守卫拦截。

### 6.3 库存管理

| 方法 | 路由 | 管理员 | 员工 |
|------|------|--------|------|
| GET | `/api/merchant/bearings` | 可查看 | 可查看 |
| POST | `/api/merchant/bearings` | 可操作 | 可操作 |
| PUT | `/api/merchant/bearings/{id:guid}` | 可操作 | 可操作(仅自己添加的) |
| POST | `/api/merchant/bearings/{id:guid}/onshelf` | 可操作 | 可操作 |
| POST | `/api/merchant/bearings/{id:guid}/offshelf` | 可操作 | 可操作 |
| PUT | `/api/merchant/bearings/{id:guid}/price-visibility` | 可操作 | 可操作 |
| POST | `/api/merchant/inventory/import` | 可操作 | 拒绝 |

> v1.14.0 说明：`POST /api/merchant/inventory/import` 为新增 Excel 批量导入端点（仅商户管理员，`.DisableAntiforgery()`）。API 用 sync-client 客户端凭据换 `api:sync` token 后转发到 Sync `POST /api/inventory/import`，写库 `DataSourceType=Manual`（不被爬虫覆盖）。配置见 appsettings.json `Sync` 节。

---

## 7. 后台管理端点 `/api/admin`（60 个）

组授权：`RequireAuthorization("Admin")`。所有端点通过 `RequirePermission("bearing.xxx")` 或 `RequireRole("SuperAdmin,Admin")` 控制访问。

### 7.1 仪表盘

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/dashboard/stats` | `dashboard.view` |

### 7.2 轴承管理

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/bearings` | `bearing.view` |
| POST | `/api/admin/bearings` | `bearing.create` |
| PUT | `/api/admin/bearings/{id:guid}` | `bearing.edit` |
| DELETE | `/api/admin/bearings/{id:guid}` | `bearing.delete` |
| PUT | `/api/admin/bearings/{id:guid}/restore` | `data.restore` |
| DELETE | `/api/admin/bearings/{id:guid}/hard` | `data.harddelete` |

### 7.3 品牌管理

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/brands` | `bearing.view` |
| POST | `/api/admin/brands` | `bearing.create` |
| PUT | `/api/admin/brands/{id:guid}` | `bearing.edit` |
| DELETE | `/api/admin/brands/{id:guid}` | `bearing.delete` |
| PUT | `/api/admin/brands/{id:guid}/restore` | `data.restore` |
| DELETE | `/api/admin/brands/{id:guid}/hard` | `data.harddelete` |

### 7.4 轴承类型管理

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/bearing-types` | `bearing.view` |
| POST | `/api/admin/bearing-types` | `bearing.create` |
| PUT | `/api/admin/bearing-types/{id:guid}` | `bearing.edit` |
| DELETE | `/api/admin/bearing-types/{id:guid}` | `bearing.delete` |
| PUT | `/api/admin/bearing-types/{id:guid}/restore` | `data.restore` |
| DELETE | `/api/admin/bearing-types/{id:guid}/hard` | `data.harddelete` |

### 7.5 商家管理

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/merchants` | `merchant.view` |
| GET | `/api/admin/merchants/{id:guid}` | `merchant.manage` |
| POST | `/api/admin/merchants` | `merchant.manage` |
| PUT | `/api/admin/merchants/{id:guid}` | `merchant.manage` |
| DELETE | `/api/admin/merchants/{id:guid}` | `merchant.manage` |
| PUT | `/api/admin/merchants/{id:guid}/restore` | `data.restore` |
| DELETE | `/api/admin/merchants/{id:guid}/hard` | `data.harddelete` |
| POST | `/api/admin/merchants/{id:guid}/approve` | `merchant.verify` |
| POST | `/api/admin/merchants/{id:guid}/members` | `merchant.verify` |
| POST | `/api/admin/merchants/{id:guid}/verify` | `merchant.verify` |
| POST | `/api/admin/merchants/{id:guid}/reject` | `merchant.verify` |

> `GET /api/admin/merchants` 支持查询参数：keyword（名称/公司名搜索）、city、type（MerchantType）、verifiedOnly（仅认证商家）、status（MerchantStatus：0=Active、1=Suspended、2=Pending、3=Draft）、excludeCrawler（bool，排除爬虫来源商家，仅显示入驻申请商家）、includeDeleted、page、pageSize。

> v1.14.0 说明：
> - `POST /api/admin/merchants/{id}/approve`：审核通过入驻申请，商户 Pending → Active 生效（补齐原缺失的审核门）；与 verify 认证分离。
> - `POST /api/admin/merchants/{id}/members`：平台指定商户成员（商户无在职管理员等异常时兜底指定/恢复成员，已存在成员行则恢复/改角色，否则新建；复用 merchant.verify 权限）。
> - `POST /api/admin/merchants/{id}/verify` 仅标记商户为已认证，不再清除爬虫 MerchantBearing 数据。

### 7.6 营业执照管理

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/licenses/pending` | `merchant.verify` |
| POST | `/api/admin/licenses/{id:guid}/approve` | `merchant.verify` |
| POST | `/api/admin/licenses/{id:guid}/reject` | `merchant.verify` |

### 7.7 纠错管理

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/corrections` | `correction.review` |
| GET | `/api/admin/corrections/pending` | `correction.review` |
| GET | `/api/admin/corrections/{id:guid}` | `correction.review` |
| POST | `/api/admin/corrections/{id:guid}/approve` | `correction.review` |
| POST | `/api/admin/corrections/{id:guid}/reject` | `correction.review` |

### 7.8 用户角色管理

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| POST | `/api/admin/users/{userId:guid}/roles` | `user.manage` |
| DELETE | `/api/admin/users/{userId:guid}/roles/{roleName}` | `user.manage` |
| GET | `/api/admin/users/{userId:guid}/roles` | `user.manage` |
| GET | `/api/admin/users/{userId:guid}/permissions` | `user.manage` |

### 7.9 角色管理

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

### 7.10 权限管理

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/permissions` | `role.manage` |
| GET | `/api/admin/permissions/{id:guid}` | `role.manage` |
| POST | `/api/admin/permissions` | `role.manage` |
| PUT | `/api/admin/permissions/{id:guid}` | `role.manage` |
| DELETE | `/api/admin/permissions/{id:guid}` | `role.manage` |

### 7.11 审计日志

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/audit-logs` | `audit.view` |

### 7.12 系统配置

| 方法 | 路由 | RequirePermission |
|------|------|-------------------|
| GET | `/api/admin/config` | `system.view` |
| PUT | `/api/admin/config/{key}` | `system.manage` |
| POST | `/api/admin/cache/refresh-rate-limit` | `system.manage` |
| GET | `/api/admin/config/price` | `system.view` |
| GET | `/api/config/reliability` | 需认证（SyncClient/Admin 等任意有效令牌），无需 system.view；返回可信度三阈值，供 Sync 运行时拉取 |

---

## 8. 同步端点 `/api/sync`（6 个）

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

## 9. 端口配置

| 环境 | HTTP | HTTPS |
|------|------|-------|
| 开发 | 5183 | 7183 |
| 生产 | 8080 | — |

---

## 10. 健康检查端点

| 端点 | 说明 |
|------|------|
| `/health` | 详细 JSON 健康检查 |
| `/healthz` | 纯文本健康检查 |
| `/health/live` | K8s 存活探针 |
| `/health/ready` | K8s 就绪探针 |