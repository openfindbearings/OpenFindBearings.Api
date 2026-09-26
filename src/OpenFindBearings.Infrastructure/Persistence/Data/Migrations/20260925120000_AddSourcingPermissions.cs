using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    [Microsoft.EntityFrameworkCore.Migrations.Migration("20260925120000_AddSourcingPermissions")]
    public partial class AddSourcingPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // v1.35.0：寻货治理权限键（sourcing.view 查看 / sourcing.manage 下架）。
            //   SeedData 对存量库整体短路，新权限必须走迁移补插（同 merchant.detach 教训）；
            //   幂等 NOT EXISTS；Admin 角色授权子查询关联不硬编码 Guid；
            //   Operator 只给查看（三员轻量：操作员日常治理可见，下架动作留 Admin）
            migrationBuilder.Sql(@"
INSERT INTO ""Permissions"" (""Id"", ""Name"", ""Description"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), v.n, v.d, now(), true
FROM (VALUES
    ('sourcing.view', '查看寻货'),
    ('sourcing.manage', '寻货治理（下架）')
) AS v(n, d)
WHERE NOT EXISTS (SELECT 1 FROM ""Permissions"" WHERE ""Name"" = v.n);

INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"", ""Id"", ""CreatedAt"", ""IsActive"")
SELECT r.""Id"", p.""Id"", gen_random_uuid(), now(), true
FROM ""Roles"" r, ""Permissions"" p
WHERE ((r.""Name"" = 'Admin' AND p.""Name"" IN ('sourcing.view', 'sourcing.manage'))
    OR (r.""Name"" = 'Operator' AND p.""Name"" = 'sourcing.view'))
  AND NOT EXISTS (
    SELECT 1 FROM ""RolePermissions"" rp
    WHERE rp.""RoleId"" = r.""Id"" AND rp.""PermissionId"" = p.""Id""
  );");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM ""RolePermissions"" WHERE ""PermissionId"" IN (SELECT ""Id"" FROM ""Permissions"" WHERE ""Name"" IN ('sourcing.view', 'sourcing.manage'));
DELETE FROM ""Permissions"" WHERE ""Name"" IN ('sourcing.view', 'sourcing.manage');");
        }
    }
}
