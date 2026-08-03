## OpenFindBearings.Api 总体设计文档 v1.1.0

**文档信息**

| 项目 | 内容 |
|------|------|
| 文档名称 | OpenFindBearings.Api 总体设计文档 |
| 版本 | v1.1.0 |
| 最后更新 | 2026-07-06 |

---

### 1. 项目定位

OpenFindBearings.Api 是轴承查找平台的**业务主系统**，提供所有面向用户和后台管理的 API。采用 DDD 四层架构 + CQRS 模式。作为 Medallion Architecture 的 Gold 层，接收 FindBearings.Sync L 阶段清洗后的业务数据，对外提供统一查询和操作接口。

### 2. 技术栈

| 层次 | 技术 | 说明 |
|------|------|------|
| 框架 | .NET 10 Minimal API | 非控制器模式，端点定义在 `Endpoints/` 下 |
| 数据库 | PostgreSQL + EF Core 10 | Npgsql.EntityFrameworkCore.PostgreSQL |
| 认证 | JWT Bearer (OpenIddict) | 验证 Identity 服务签发的 token |
| 缓存 | MemoryCache + Redis（可选） | 两级缓存，Redis 可选启用 |
| 消息 | Channel<T> 后台任务队列 | 异步日志写入 |
| 熔断 | Polly | HTTP 重试策略 |

### 3. 四层架构

```
src/
  OpenFindBearings.Api/             -- 表示层 (Minimal API)
  OpenFindBearings.Application/     -- 应用层 (CQRS Commands/Queries)
  OpenFindBearings.Domain/          -- 领域层 (实体 / 聚合根 / 值对象 / 仓储接口)
  OpenFindBearings.Infrastructure/  -- 基础设施层 (EF Core / 仓储实现 / 外部服务)
```

依赖方向：Api → Application → Domain ← Infrastructure

- **Domain 层**：仅依赖 MediatR.Contracts，无 EF Core 或数据库依赖
- **Application 层**：依赖 Domain，通过 MediatR 组织 CQRS
- **Infrastructure 层**：实现仓储接口，提供 DbContext、HTTP 客户端、缓存等
- **Api 层**：组合一切，启动 Minimal API 端点

### 4. 领域模型概览

**聚合根：**
- `Bearing` — 轴承（核心聚合，含尺寸/性能参数/品牌/类型/替代品/商家关联）
- `Merchant` — 商家（含员工、认证状态、等级）
- `User` — 业务用户（通过 AuthUserId 关联 Identity 用户）

**实体：**
- `Brand` — 品牌字典（已删除 Website/Description/SortOrder 字段）
- `BearingType` — 轴承类型字典（已删除 SortOrder 字段）
- `BearingInterchange` — 轴承替代品关系（含 Confidence 可信度评分）
- `MerchantBearing` — 商家-轴承关联（价格/库存/上架状态/来源标记 DataSource）
- `CorrectionRequest` — 纠错请求
- `AuditLog` — 管理员操作审计日志
- `ApiCallLog` — API 调用日志
- `SystemConfig` — 系统配置键值存储

**值对象：**
- `Dimensions` — 内径/外径/宽度（mm）
- `PerformanceParams` — 动/静载荷（kN）、极限转速（rpm，int?）
- `DataSource` — 数据来源类型（Manual/Crawler/FileImport/Api/SeedData）
- `ContactInfo` — 联系人信息（ContactPerson/Phone/Mobile/Email/Address）

### 5. 中间件管道

按执行顺序：

1. `ExceptionHandlingMiddleware` — 全局异常处理（映射 FluentValidation、DbUpdateException 等）
2. `RateLimitingMiddleware` — 用户级速率限制；`/api/sync/*` 路径白名单放行（M2M 接口不限流）
3. `ApiLoggingMiddleware` — 异步 API 调用日志（通过 Channel 队列写入）
4. 认证 → 授权 → CORS → 端点映射

### 6. 构建与运行

```bash
cd OpenFindBearings.Api
dotnet restore .\OpenFindBearings.Api.slnx
dotnet build .\OpenFindBearings.Api.slnx
dotnet run --project .\src\OpenFindBearings.Api
```

数据库迁移：

```bash
dotnet ef migrations add <Name> --project src/OpenFindBearings.Infrastructure \
    --startup-project src/OpenFindBearings.Api
dotnet ef database update --project src/OpenFindBearings.Infrastructure \
    --startup-project src/OpenFindBearings.Api
```

### 7. 关键设计决策

1. **充血模型实体**：所有实体通过工厂方法创建，业务逻辑封装在实体内部
2. **全局软删除**：`HasQueryFilter(b => b.IsActive)` 应用于 Brand/BearingType/Bearing/BearingInterchange/MerchantBearing；Merchant.User 的 IsActive 过滤在 LINQ 查询中手动处理（无 HasQueryFilter）
3. **JWT 多租户**：AuthUserId 桥接 Identity 服务的 OidcUser，业务用户独立管理
4. **Sync 客户端专用策略**：`RequireAuthorization("SyncClient")` 要求 `scope=api:sync` claim
5. **覆盖保护**：BatchCreate 端点检查 DataSource，非 Crawler 来源跳过覆盖
6. **异步日志**：API 日志通过 Channel<T> 队列异步写入
7. **商户入驻设计**：爬虫数据作为冷启动种子数据；L 阶段加载 MerchantBearing 时跳过已认证商户

### 8. 版本历史

| 版本 | 日期 | 变更说明 |
|------|------|----------|
| v1.0.0 | 2026-06-06 | 初始版本 |
| v1.1.0 | 2026-07-06 | MerchantBearing 补充 DataSource 字段；Merchant 补充 EnglishName/Verify(verifiedBy)；性能参数补充 LimitingSpeedOil(int?)；移除 UserPreference/LicenseVerification 实体引用；中间件管道移除未实现的 UserContextMiddleware；RateLimiting 补充 /api/sync/* 白名单说明；软删除仅适用于部分实体；新增覆盖保护和商户入驻设计决策 |
