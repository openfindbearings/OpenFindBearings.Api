# API 认证集成设计文档

**版本：** v1.0.0  
**日期：** 2026-06-12  
**状态：** 实施中

---

## 1. 概述

OpenFindBearings.Api 作为后端数据服务，通过 JWT Bearer 验证来自 Identity 认证中心的 token，并通过 PermissionFilter 进行细粒度权限校验。

## 2. 认证架构

```
客户端（Admin/移动端）
    │
    ├── 携带 JWT Bearer Token
    │
    ▼
API 请求入口
    │
    ├── JWT Bearer 中间件验证 token 有效性
    ├── RequireAuthorization("Admin") 要求认证
    │
    ▼
Endpoint Filter
    │
    ├── PermissionEndpointFilter → 校验具体权限
    └── RoleEndpointFilter → 校验角色
```

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

## 4. 授权策略

### 4.1 策略定义

| 策略名 | 要求 | 用途 |
|--------|------|------|
| Admin | RequireAuthenticatedUser() | Admin 管理端点（配合 RequirePermission） |
| Merchant | RequireAuthenticatedUser() | 商家端点 |
| SyncClient | RequireClaim("scope", "api:sync") | 同步客户端（M2M） |
| Authenticated | RequireAuthenticatedUser() | 通用认证端点 |

### 4.2 AdminEndpoints 授权

```csharp
// Program.cs
var adminGroup = api.MapGroup("/api/admin")
    .RequireAuthorization("Admin");  // v1.1.0 恢复

// 各端点通过 RequirePermission 进行细粒度权限校验
adminGroup.MapGet("/brands", GetAllBrands)
    .RequirePermission("bearing.view");
```

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

## 6. 开发环境认证（v1.1.0 移除）

### 6.1 已移除的 Dev Bypass

v1.1.0 移除了 `PermissionEndpointFilter` 和 `RoleEndpointFilter` 中的 `IsDevelopment()` bypass。开发环境通过正常登录流程获取 JWT token。

### 6.2 迁移说明

| 版本 | 开发环境行为 |
|------|-------------|
| v1.0.0 | `IsDevelopment() && !IsAuthenticated` 时自动放行 |
| v1.1.0 | 移除 bypass，所有环境必须携带有效 JWT |

开发环境调试方式：
1. 启动 Identity 服务（端口 7201）
2. 启动 Admin 服务（端口 7167）
3. 访问 Admin 页面，自动跳转 Identity 登录
4. 登录后 Admin 获取 JWT，API 请求自动携带

## 7. 端点权限标注

### 7.1 RequirePermission 标注示例

```csharp
// 管理端点
.MapGet("/brands", GetAllBrands)
    .RequirePermission("bearing.view")

.MapPost("/brands", CreateBrand)
    .RequirePermission("bearing.create")

.MapPut("/brands/{id}", UpdateBrand)
    .RequirePermission("bearing.edit")

.MapDelete("/brands/{id}", DeleteBrand)
    .RequirePermission("bearing.delete")

.MapDelete("/brands/{id}/hard", HardDeleteBrand)
    .RequirePermission("data.harddelete")

.MapPut("/brands/{id}/restore", RestoreBrand)
    .RequirePermission("data.restore")
```

### 7.2 权限与端点映射

详见 `OpenFindBearings.Api/doc/04-用户角色权限/用户角色权限设计-v1.1.0.md` 第 2.1 节。

## 8. 与其他项目的边界

| 项目 | 职责 |
|------|------|
| Identity | 签发 JWT token，验证 token 有效性 |
| Admin | 用户登录、Cookie 存储 JWT、携带 JWT 调用 API |
| API | 验证 JWT、校验权限、返回数据 |

## 9. 端口配置

| 环境 | HTTP | HTTPS |
|------|------|-------|
| 开发 | 5183 | 7183 |
| 生产 | 8080 | — |

## 10. 实施清单

### 已完成

- [x] JWT Bearer 中间件配置
- [x] 授权策略定义（Admin/Merchant/SyncClient/Authenticated）
- [x] PermissionEndpointFilter
- [x] RoleEndpointFilter
- [x] AdminEndpoints RequirePermission 标注

### 实施中

- [x] 移除 Dev Bypass
- [x] 恢复 AdminEndpoints RequireAuthorization("Admin")

### 后续优化

- [ ] 审计日志记录
- [ ] Token 黑名单（登出时吊销）
