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