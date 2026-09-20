## OpenFindBearings.Api 总体设计文档 v1.5.0

**文档信息**

| 项目 | 内容 |
|------|------|
| 文档名称 | OpenFindBearings.Api 总体设计文档 |
| 版本 | v1.5.0 |
| 最后更新 | 2026-09-20 |

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
| 对象存储 | MinIO（S3 协议）+ AWSSDK.S3 | 用户上传文件统一落 bucket=media（uploads/**），开发可切本地盘 |

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
- `MerchantMember` — 商户成员（UserId/MerchantId/Role/Status，一人多商户归属表）
- `CorrectionRequest` — 纠错请求
- `AuditLog` — 管理员操作审计日志
- `ApiCallLog` — API 调用日志
- `SystemConfig` — 系统配置键值存储

**值对象：**
- `Dimensions` — 内径/外径/宽度（mm）
- `PerformanceParams` — 动/静载荷（kN）、极限转速（rpm，int?）
- `DataSource` — 数据来源类型（Manual/Crawler/FileImport/Api/SeedData）
- `ContactInfo` — 联系人信息（ContactPerson/Phone/Mobile/Email/Address/QQ）

### 5. 中间件管道

实际管道顺序（`Program.cs`）：

```
1. ExceptionHandlingMiddleware     ← 全局异常处理（映射 FluentValidation、DbUpdateException、NpgsqlException 等；DB 连接失败返回 503）
   UseHttpsRedirection()           ← HTTP→HTTPS
   UseCors("AllowSpecificOrigins") ← 跨域策略
   UseAuthentication()             ← 认证
2. UserContextMiddleware           ← 在认证之后、授权之前，解析用户身份写入 HttpContext.Items
3. ApiLoggingMiddleware            ← 异步 API 调用日志（通过 Channel 队列写入），在 UserContext 之后可获取真实用户身份
4. RateLimitingMiddleware          ← 用户级速率限制；/api/sync/* 路径白名单放行；在 UserContext 之后可按真实角色分配配额
   UseAuthorization()              ← 授权
   UseResponseCompression()        ← 响应压缩
5. AuditLogMiddleware              ← 审计日志（拦截 POST/PUT/PATCH/DELETE，跳过 GET/HEAD/OPTIONS 及 /health、/swagger 路径）
   UseStaticFiles()                ← 静态文件（robots.txt）
   MapApiEndpoints()               ← 端点映射
   MapAllMapHealthChecks()         ← 健康检查
```

> 注：v1.1.0 曾记录"移除未实现的 UserContextMiddleware"，但代码中已实现并注册。ApiLogging 与 RateLimiting 的顺序也与 v1.1.0 描述相反——代码中 ApiLogging 在前（先记录日志），RateLimiting 在后（限流判定依赖 UserContext 解析后的真实身份）。响应压缩（ResponseCompression）在代码中存在但文档未提及。

### 6. 构建与运行

```bash
cd OpenFindBearings.Api
dotnet restore .\OpenFindBearings.Api.slnx
dotnet build .\OpenFindBearings.Api.slnx
dotnet run --project .\src\OpenFindBearings.Api
```

数据库迁移：**生产由启动自动应用（见第 9 节）**，以下 `dotnet ef` 命令仅用于本地开发或排障时手动执行：

```bash
dotnet ef migrations add <Name> --project src/OpenFindBearings.Infrastructure \
    --startup-project src/OpenFindBearings.Api
dotnet ef database update --project src/OpenFindBearings.Infrastructure \
    --startup-project src/OpenFindBearings.Api
```

### 7. 关键设计决策

- **上传统一走 `IObjectStorageService` 抽象（v1.5.0）**：头像/Logo/证照材料等用户上传不再直写容器 wwwroot，
  生产落 MinIO（S3 标准协议，AWS SDK 客户端），开发落本地盘，`FileStorage:Provider` 一键切换；
  库内仍存相对键 `/uploads/...`，媒体服务 nginx 反代 MinIO 直出，客户端零改动。
  动因：hostPath 卷不支持 fsGroup 权限托管，initContainer chown hack 曾引发线上上传 Access denied。
  部署、迁移与回滚全流程见 `doc/09-MinIO对象存储设计/MinIO对象存储设计与迁移指南-v1.0.0.md`。
- **`IFileService/LocalFileService` 死代码删除（v1.5.0）**：注册后从未被任何端点消费，随对象存储抽象一并清理。

1. **充血模型实体**：所有实体通过工厂方法创建，业务逻辑封装在实体内部
2. **全局软删除**：`HasQueryFilter(b => b.IsActive)` 应用于 Brand/BearingType/Bearing/BearingInterchange/MerchantBearing；Merchant.User 的 IsActive 过滤在 LINQ 查询中手动处理（无 HasQueryFilter）
3. **JWT 多租户**：AuthUserId 桥接 Identity 服务的 OidcUser，业务用户独立管理
4. **Sync 客户端专用策略**：`RequireAuthorization("SyncClient")` 要求 `scope=api:sync` claim
5. **覆盖保护**：BatchCreate 端点检查 DataSource，非 Crawler 来源跳过覆盖
6. **异步日志**：API 日志通过 Channel<T> 队列异步写入
7. **商户入驻设计**：爬虫数据作为冷启动种子数据；L 阶段加载 MerchantBearing 时跳过已认证商户
8. **启动自动迁移**：Schema 由应用启动时统一自动追平（详见第 9 节），杜绝"库落后于代码"的部署事故

### 8. 数据库迁移策略

**背景事故**：商户入驻 P1 之后的 `AddMerchantMembers`、`AddBearingInterchangeDataSource` 两条迁移，部署时未在目标库执行，生产 `db_api` 停留在 `InitialCreate`、缺 `MerchantMembers` 表；而新版 API 代码的认领搜索 `GetClaimableAsync` 会 join 该表 → 运行时 Postgres `relation does not exist` → 接口 500 → BFF 兜底成空列表（表现为"搜洛阳无结果"，实际数据正常且 `DataSourceType='Crawler'`）。

**根因**：迁移原先放在 `SeedData.SeedAsync` 里，且整段被 `try/catch` 包裹、异常仅记日志后继续启动。一次迁移失败被静默吞掉，服务照常对外提供，掩盖了 schema 缺失。

**现行策略（v1.4.0 起）**：

1. **迁移单一入口**：`Program.cs` 在 `app.Build()` 之后、注册中间件与 `app.Run()` 之前，用 scope 解析 `ApplicationDbContext`，检测 `GetPendingMigrations()`，有待应用则 `MigrateAsync()`。
2. **Fail-fast**：迁移失败**直接抛出、使进程启动失败**（K8s 下 Pod 进入 CrashLoopBackOff 并告警），而非带错误 schema 静默服务，避免"500 掩盖成空结果"。
3. **职责分离**：`SeedData.SeedAsync` 不再执行迁移，仅负责种子数据；其 `try/catch` 只兜种子阶段的异常。迁移与种子互不吞异常。
4. **幂等可重复**：依赖 EF 的 `__EFMigrationsHistory`，已应用不重复执行。
5. **重发版本即修复**：本次改动随新版本发布，下次部署启动即自动补上生产缺失的两条迁移（含建 `MerchantMembers` 表与按 `Users.MerchantId` 回填成员）。

**多副本注意事项**：当前为单副本，启动直接 `MigrateAsync` 安全。若将来扩到多副本并使用滚动更新，新旧 Pod 会短暂重叠并发迁移（EF `Migrate` 默认不加跨进程锁），届时需二选一加固：迁移前取 Postgres advisory lock（`pg_advisory_lock`）串行化，或将迁移上移为部署流水线里的一次性 K8s Migration Job（应用 Pod 不再执行 DDL）。

**权限前提**：API 连库用户需具备 DDL 权限（CREATE/ALTER）；目标库 `db_api` 各表 owner 为 `u_api`，符合。

### 9. 版本历史

| 版本 | 日期 | 变更说明 |
|------|------|----------|
| v1.0.0 | 2026-06-06 | 初始版本 |
| v1.1.0 | 2026-07-06 | MerchantBearing 补充 DataSource 字段；Merchant 补充 EnglishName/Verify(verifiedBy)；性能参数补充 LimitingSpeedOil(int?)；移除 UserPreference/LicenseVerification 实体引用；中间件管道移除未实现的 UserContextMiddleware；RateLimiting 补充 /api/sync/* 白名单说明；软删除仅适用于部分实体；新增覆盖保护和商户入驻设计决策 |
| v1.3.0 | 2026-09-11 | 代码审查对齐：修正中间件管道顺序（UserContextMiddleware 已实现并注册、ApiLogging 与 RateLimiting 顺序与代码对齐、补充 ResponseCompression 和 UseStaticFiles）；值对象 ContactInfo 补充 QQ 字段；领域模型概览与实际代码对齐 |
| v1.4.0 | 2026-09-13 | 新增"数据库迁移策略"章节并升为第 8 节（版本历史顺延为第 9 节）：记录生产库停在 InitialCreate、缺 MerchantMembers 表导致认领搜索 500 的事故根因（SeedData 吞异常）；迁移改为 Program.cs 启动统一自动应用、失败即崩（fail-fast），SeedData 不再迁移只留种子；关键设计决策补第 8 项"启动自动迁移"；构建运行第 6 节标注 ef 命令仅本地/排障用；领域模型概览补充 MerchantMember 实体；注明多副本下需 advisory lock 或部署前一次性迁移 Job 的加固方案 |
