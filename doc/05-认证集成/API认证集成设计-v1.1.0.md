# API 认证集成设计文档

**版本：** v1.1.0
**日期：** 2026-07-06
**状态：** 待实施（商户入驻体系设计完成后同步实施）

---

## 变更记录

| 版本 | 日期 | 变更说明 |
|------|------|----------|
| v1.0.0 | 2026-06-12 | 初始版本：JWT Bearer 配置、授权策略定义、PermissionEndpointFilter |
| v1.1.0 | 2026-07-06 | 新增商户认证体系：Merchant 策略补充、Mobile 客户端认证流程、商户身份解析说明 |

---

## 1. 概述

OpenFindBearings.Api 作为后端数据服务，通过 JWT Bearer 验证来自 Identity 认证中心的 token，并通过 PermissionFilter 进行细粒度权限校验。

目前有三个客户端类型：

| 客户端 | 认证方式 | OAuth 流程 | 授权策略 |
|--------|----------|------------|----------|
| Admin 后台 | 用户名+密码 | authorization_code | Admin + Permission |
| Mobile APP | 手机号+短信验证码 | sms (custom grant) | Merchant / Authenticated |
| Sync 服务 | client_credentials | client_credentials | SyncClient (scope api:sync) |

---

## 2. 认证架构

```
客户端（Admin/移动端/Sync）
    │
    ├── 携带 JWT Bearer Token
    │
    ▼
API 请求入口
    │
    ├── JWT Bearer 中间件验证 token 有效性
    ├── RequireAuthorization("Admin") / "Merchant" / "SyncClient"
    │
    ▼
Endpoint Filter
    │
    ├── PermissionEndpointFilter → 校验具体权限（仅 Admin 端点）
    └── RoleEndpointFilter → 校验角色（仅 Admin 端点）
```

token 签发全部由 OpenFindBearings.Identity 负责：

| 客户端 | 获取 token 的方式 | 说明 |
|--------|-------------------|------|
| Admin | authorization_code 流程 | 浏览器重定向到 Identity 登录页 |
| Mobile | sms custom grant 流程 | 手机号+验证码直接获取 token |
| Sync | client_credentials | M2M 无用户交互 |

API 不区分 token 来源——无论是 authorization_code 还是 sms 签发的 JWT，在 API 端统一通过 JWT Bearer 中间件验证。

---

## 3. JWT 配置

```json
{
  "Authentication": {
    "Authority": "https://localhost:7201",
    "Audience": "api",
    "RequireHttpsMetadata": false
  }
}
```

### 3.1 Token 验证参数

| 参数 | 值 | 说明 |
|------|-----|------|
| ValidateIssuer | true | 验证签发者 |
| ValidateAudience | true | 验证受众 |
| ValidateLifetime | true | 验证有效期 |
| ValidateIssuerSigningKey | true | 验证签名密钥 |
| RoleClaimType | "role" | 角色声明类型（业务系统自行管理，不验证） |
| NameClaimType | "name" | 用户名声明类型 |

---

## 4. 授权策略

### 4.1 策略定义

```csharp
// Admin 策略
options.AddPolicy("Admin", policy =>
    policy.RequireAuthenticatedUser());

// 商家策略
options.AddPolicy("Merchant", policy =>
    policy.RequireAuthenticatedUser());

// 同步客户端策略
options.AddPolicy("SyncClient", policy =>
    policy.RequireClaim("scope", "api:sync"));

// 登录用户策略
options.AddPolicy("Authenticated", policy =>
    policy.RequireAuthenticatedUser());
```

| 策略名 | 要求 | 用途 |
|--------|------|------|
| Admin | RequireAuthenticatedUser() | Admin 管理端点（配合 RequirePermission） |
| Merchant | RequireAuthenticatedUser() | 商家端点（商户管理员/员工） |
| SyncClient | RequireClaim("scope", "api:sync") | 同步客户端（M2M） |
| Authenticated | RequireAuthenticatedUser() | 通用认证端点 |

`Merchant` 策略与 `Admin` 策略都依赖 `RequireAuthenticatedUser()`，区别在于后续校验：

| 维度 | Admin 端点 | Merchant 端点 |
|------|-----------|---------------|
| 路由前缀 | `/api/admin/*` | `/api/merchant/*` |
| 权限校验 | PermissionEndpointFilter + RequirePermission | 无（商户管理自己的数据） |
| 身份解析 | AdminUserService + admin_user_roles 表 | CurrentUserService + User.MerchantId 关联 |
| 用户要求 | Identity 任意用户 + admin_role_permissions | Identity 用户且关联到认证商户 |

### 4.2 AdminEndpoints 授权

```csharp
var adminGroup = api.MapGroup("/api/admin")
    .RequireAuthorization("Admin");

// 各端点通过 RequirePermission 进行细粒度权限校验
adminGroup.MapGet("/brands", GetAllBrands)
    .RequirePermission("bearing.view");

adminGroup.MapPost("/brands", CreateBrand)
    .RequirePermission("bearing.create");

adminGroup.MapPut("/brands/{id}", UpdateBrand)
    .RequirePermission("bearing.edit");

adminGroup.MapDelete("/brands/{id}", DeleteBrand)
    .RequirePermission("bearing.delete");

adminGroup.MapDelete("/brands/{id}/hard", HardDeleteBrand)
    .RequirePermission("data.harddelete");

adminGroup.MapPut("/brands/{id}/restore", RestoreBrand)
    .RequirePermission("data.restore");
```

### 4.3 MerchantEndpoints 授权

```csharp
var merchantGroup = app.MapGroup("/api/merchant")
    .RequireAuthorization("Merchant");
// 无需 RequirePermission——商户管理自身库存
```

MerchantEndpoints 不通过 PermissionFilter，而是通过 ICurrentUserService 获取当前用户的 UserId，再通过 GetMerchantByUserIdQuery 解析商户 ID，后续操作限定在该商户范围内：

```csharp
// 获取当前用户所属商户
if (!currentUser.UserId.HasValue)
    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

var merchantQuery = new GetMerchantByUserIdQuery
{
    UserId = currentUser.UserId.Value
};
var merchant = await mediator.Send(merchantQuery);

if (merchant == null)
    return ApiResponseHelper.NotFound("未找到所属商家", httpContext);
```

---

## 5. 权限过滤器

### 5.1 PermissionEndpointFilter

```csharp
public class PermissionEndpointFilter : IEndpointFilter
{
    private readonly string _permissionName;

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var permissionService = httpContext.RequestServices
            .GetRequiredService<IPermissionService>();

        var hasPermission = await permissionService.HasPermissionAsync(_permissionName);

        if (!hasPermission)
        {
            return Results.Forbid();
        }

        return await next(context);
    }
}
```

该过滤器仅用于 Admin 端点，校验具体权限键（`bearing.view`、`merchant.manage` 等）。Merchant 端点不使用此过滤器。

### 5.2 RoleEndpointFilter

```csharp
public class RoleEndpointFilter : IEndpointFilter
{
    private readonly string _roleName;

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var permissionService = httpContext.RequestServices
            .GetRequiredService<IPermissionService>();

        var hasRole = permissionService.HasRole(_roleName);

        if (!hasRole)
        {
            return Results.Forbid();
        }

        return await next(context);
    }
}
```

---

## 6. 开发环境认证

### 6.1 Dev Bypass 已移除

v1.0.0 阶段在 `PermissionEndpointFilter` 和 `RoleEndpointFilter` 中存在以下 bypass：

```csharp
// v1.0.0 的 Dev Bypass（已移除）
if (environment.IsDevelopment() && !httpContext.User.Identity?.IsAuthenticated == true)
{
    // 注入模拟管理员身份
    return await next(context);
}
```

v1.1.0 已彻底移除。开发环境所有请求必须携带有效 JWT。

### 6.2 开发环境调试方式

| 客户端 | 调试方式 | 所需服务 |
|--------|----------|----------|
| Admin 后台 | 浏览器访问 Admin，自动跳转 Identity 登录 | Identity (7201) + Admin (7167) |
| Mobile APP | 调用 Identity send-code → grant_type=sms 获取 token | Identity (7201) |
| Sync 服务 | 直接调用 Identity client_credentials 获取 token | Identity (7201) |

调试 API 端点的通用步骤：

```bash
# 1. 获取 token（不同客户端使用各自的流程）
# Admin：通过浏览器登录获取
# Mobile：sms grant
curl POST /connect/token -d 'grant_type=sms&phone=13800138000&code=123456'

# 2. 携带 token 调用 API
curl GET /api/admin/dashboard/stats \
  -H "Authorization: Bearer {access_token}"
```

---

## 7. 端点权限标注

### 7.1 Admin 端点权限映射

| 权限键 | 说明 | 覆盖端点 |
|--------|------|----------|
| `dashboard.view` | 查看仪表盘 | GET /api/admin/dashboard/stats |
| `bearing.view` | 查看轴承列表 | GET /api/admin/bearings, GET /api/admin/brands, GET /api/admin/bearing-types |
| `bearing.create` | 创建轴承 | POST /api/admin/bearings, POST /api/admin/brands, POST /api/admin/bearing-types |
| `bearing.edit` | 编辑轴承、恢复已删除 | PUT /api/admin/bearings/{id}, PUT /api/admin/brands/{id}, PUT /api/admin/bearing-types/{id} |
| `bearing.delete` | 删除轴承（软删除） | DELETE /api/admin/bearings/{id}, DELETE /api/admin/brands/{id}, DELETE /api/admin/bearing-types/{id} |
| `correction.review` | 信息纠错审核 | GET/POST /api/admin/corrections/* |
| `merchant.manage` | 商家 CRUD | POST/PUT/DELETE /api/admin/merchants |
| `merchant.verify` | 商家认证审核 | POST /api/admin/merchants/{id}/verify, POST /api/admin/merchants/{id}/reject, GET/POST /api/admin/licenses/* |
| `sync.trigger` | 触发 ETL 任务 | POST /api/sync/etl/* |
| `sync.view` | 查看同步状态 | GET /api/sync/status |
| `crawler.view` | 查看爬虫状态 | GET /api/crawlers, GET /api/crawlers/{name}/status |
| `crawler.trigger` | 触发爬虫运行 | POST /api/crawlers/{name}/run |
| `data.restore` | 恢复已删除数据 | PUT /api/admin/*/restore |
| `data.harddelete` | 彻底删除（超级管理员专属） | DELETE /api/admin/*/hard |
| `user.manage` | 管理用户 | /api/admin/users/* |
| `role.manage` | 管理角色和权限 | CRUD /api/admin/roles/*, /api/admin/permissions/* |
| `system.view` | 查看系统配置 | GET /api/admin/config |
| `system.manage` | 管理系统配置 | PUT /api/admin/config/{key} |
| `audit.view` | 查看审计日志 | GET /api/admin/audit-logs |

### 7.2 Merchant 端点

Merchant 端点不使用 RequirePermission，全部通过 ICurrentUserService 进行商户归属验证，操作自动限定在当前用户的商户范围内：

| 端点 | 说明 | 管理员 | 员工 |
|------|------|--------|------|
| GET /api/merchant/profile | 获取店铺信息 | 可查看 | 可查看 |
| PUT /api/merchant/profile | 更新店铺信息 | 可操作 | 拒绝 |
| POST /api/merchant/license | 上传营业执照 | 可操作 | 拒绝 |
| GET /api/merchant/staff | 获取员工列表 | 可查看 | 可查看 |
| POST /api/merchant/staff | 添加员工 | 可操作 | 拒绝 |
| DELETE /api/merchant/staff/{userId} | 移除员工 | 可操作 | 拒绝 |
| GET /api/merchant/bearings | 获取库存列表 | 可查看 | 可查看 |
| POST /api/merchant/bearings | 添加在售轴承 | 可操作 | 可操作 |
| PUT /api/merchant/bearings/{id} | 更新轴承信息 | 可操作 | 可操作 |
| POST /api/merchant/bearings/{id}/onshelf | 上架 | 可操作 | 可操作 |
| POST /api/merchant/bearings/{id}/offshelf | 下架 | 可操作 | 可操作 |
| PUT /api/merchant/bearings/{id}/price-visibility | 设置价格可见性 | 可操作 | 可操作 |

管理员和员工的区分通过 `IPermissionService.IsMerchantAdminAsync()` 实现，该接口检查当前用户在所属商户的角色是否为管理员。

### 7.3 商户入驻相关端点（新增）

| 端点 | 说明 | 认证 |
|------|------|------|
| POST /api/merchant/apply | 提交入驻申请（认领/新建） | Bearer（任意登录用户） |
| GET /api/merchant/application-status | 查看入驻申请状态 | Bearer（申请用户） |
| POST /api/admin/merchants/{id}/reject | 拒绝入驻申请（含原因） | Bearer + merchant.verify |
| GET /api/admin/licenses/{id} | 预览营业执照 | Bearer + merchant.verify |
| GET /api/admin/merchants/{id}/audit-history | 审核记录 | Bearer + merchant.verify |

---

## 8. 与其他项目的边界

| 项目 | 职责 |
|------|------|
| Identity | 签发 JWT token（含 sms grant_type），验证 token 有效性 |
| Admin | 用户登录、Cookie 存储 JWT、携带 JWT 调用 API，入驻审核 UI |
| Mobile APP | 手机号注册/登录（sms grant_type）、商户入驻、库存管理 |
| API | 验证 JWT、校验权限、返回数据 |
| Sync | 通过 client_credentials 获取 token，调用 API 同步数据 |

---

## 9. 端口配置

| 环境 | HTTP | HTTPS |
|------|------|-------|
| 开发 | 5183 | 7183 |
| 生产 | 8080 | — |

---

## 10. 实施清单

### 已完成

- [x] JWT Bearer 中间件配置
- [x] 授权策略定义（Admin/Merchant/SyncClient/Authenticated）
- [x] PermissionEndpointFilter
- [x] RoleEndpointFilter
- [x] AdminEndpoints RequirePermission 标注
- [x] MerchantEndpoints 基础实现（profile/staff/bearings CRUD）
- [x] 移除 Dev Bypass
- [x] 恢复 AdminEndpoints RequireAuthorization("Admin")

### 待实施（商户入驻体系）

- [ ] SMS 自定义授权流程（Identity 侧）
- [ ] MerchantEndpoints 补充入驻申请端点
- [ ] MerchantEndpoints 补充员工邀请端点
- [ ] Merchant 入驻审核接口补充
- [ ] Admin 入驻审核 UI 补齐

### 后续优化

- [ ] 审计日志记录
- [ ] Token 黑名单（登出时吊销）
