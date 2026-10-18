using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <summary>
    /// v2.17.0 替代品字典化：为平台维护的替代关系（主数据字典）新增独立权限键，
    /// 替代此前借用 merchant.verify（认证审核语义）的做法。
    /// 目录变更必须走迁移：SeedData 对存量库整体短路，新增权限键需在已有库上补插。
    /// </summary>
    /// <inheritdoc />
    public partial class AddInterchangePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 新权限键 interchange.view（查看替代关系）/ interchange.manage（维护替代关系）；
            // 幂等 NOT EXISTS；角色授权用子查询关联角色名，不硬编码 Guid；
            // 授权口径：Admin 全量、Operator 查看 + 维护（主数据日常维护）、Auditor 仅查看（只读监察）
            migrationBuilder.Sql(@"
INSERT INTO ""Permissions"" (""Id"", ""Name"", ""Description"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), v.n, v.d, now(), true
FROM (VALUES
    ('interchange.view', '查看替代关系'),
    ('interchange.manage', '维护替代关系')
) AS v(n, d)
WHERE NOT EXISTS (SELECT 1 FROM ""Permissions"" WHERE ""Name"" = v.n);

INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"", ""Id"", ""CreatedAt"", ""IsActive"")
SELECT r.""Id"", p.""Id"", gen_random_uuid(), now(), true
FROM ""Roles"" r, ""Permissions"" p
WHERE ((r.""Name"" = 'Admin' AND p.""Name"" IN ('interchange.view', 'interchange.manage'))
    OR (r.""Name"" = 'Operator' AND p.""Name"" IN ('interchange.view', 'interchange.manage'))
    OR (r.""Name"" = 'Auditor' AND p.""Name"" = 'interchange.view'))
  AND NOT EXISTS (
    SELECT 1 FROM ""RolePermissions"" rp
    WHERE rp.""RoleId"" = r.""Id"" AND rp.""PermissionId"" = p.""Id""
  );");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 回滚：先删角色授权再删权限键
            migrationBuilder.Sql(@"
DELETE FROM ""RolePermissions"" WHERE ""PermissionId"" IN (SELECT ""Id"" FROM ""Permissions"" WHERE ""Name"" IN ('interchange.view', 'interchange.manage'));
DELETE FROM ""Permissions"" WHERE ""Name"" IN ('interchange.view', 'interchange.manage');");
        }
    }
}
