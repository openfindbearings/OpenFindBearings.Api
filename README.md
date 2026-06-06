# OpenFindBearings.Api

轴承查询与商家信息管理系统 API

## 技术栈

- .NET 10.0
- ASP.NET Core
- Entity Framework Core (PostgreSQL)
- MediatR (CQRS)
- FluentValidation

## 项目结构

```
src/
├── OpenFindBearings.Api           # Web API 项目
├── OpenFindBearings.Application   # 应用层 (Commands/Queries)
├── OpenFindBearings.Domain        # 领域层 (Entities/Value Objects)
└── OpenFindBearings.Infrastructure # 基础设施层 (Repository/Services)
```

## 配置

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=db_api;Username=user_api;Password=111111"
  },
  "Authentication": {
    "Authority": "https://auth.abcsxl.com",
    "Audience": "openfindbearings-api",
    "RequireHttpsMetadata": true
  },
  "CacheSettings": {
    "EnableRedis": false,
    "RedisConnectionString": "localhost:6379",
    "DefaultExpirationMinutes": 60
  },
  "FileStorage": {
    "BasePath": "uploads",
    "BaseUrl": "/uploads",
    "MaxFileSize": 10485760,
    "AllowedExtensions": [ ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg", ".dwg", ".dxf" ]
  }
}
```

## 数据库迁移

```bash
# 添加迁移
dotnet ef migrations add <MigrationName> -p src/OpenFindBearings.Infrastructure --startup-project src/OpenFindBearings.Api

# 执行迁移
dotnet ef database update -p src/OpenFindBearings.Infrastructure --startup-project src/OpenFindBearings.Api
```

## 近期更新 (2026-04)

- 新增轴承图片字段 (Image3D, Image2DCAD)
- 新增文件上传服务 (IFileService/LocalFileService)
- 搜索查询增加必须提供搜索条件校验
- 仓储层增加分页参数校验 (Page >= 1, 1 <= PageSize <= 100)
- 移除未使用的 BearingSearchParams.Validate() 方法