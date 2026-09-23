using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMerchantDetachPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // v2.17.0：Admin 强制解除商户归属权限键。SeedData 对存量库整体短路（SystemConfigs 非空即跳过），
            //   新权限必须走迁移补插，否则存量环境 merchant.detach 永远 403。
            //   幂等写法：NOT EXISTS 防重复执行；Admin 角色授权用子查询关联（不硬编码 Guid）。
            //   新环境由 SeedData 直接创建（权限清单已同步追加），本迁移对其为无害 no-op。
            migrationBuilder.Sql(@"
INSERT INTO ""Permissions"" (""Id"", ""Name"", ""Description"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), 'merchant.detach', '解除商户归属', now(), true
WHERE NOT EXISTS (SELECT 1 FROM ""Permissions"" WHERE ""Name"" = 'merchant.detach');

INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"", ""Id"", ""CreatedAt"", ""IsActive"")
SELECT r.""Id"", p.""Id"", gen_random_uuid(), now(), true
FROM ""Roles"" r, ""Permissions"" p
WHERE r.""Name"" = 'Admin' AND p.""Name"" = 'merchant.detach'
  AND NOT EXISTS (
    SELECT 1 FROM ""RolePermissions"" rp
    WHERE rp.""RoleId"" = r.""Id"" AND rp.""PermissionId"" = p.""Id""
  );");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM ""RolePermissions"" WHERE ""PermissionId"" IN (SELECT ""Id"" FROM ""Permissions"" WHERE ""Name"" = 'merchant.detach');
DELETE FROM ""Permissions"" WHERE ""Name"" = 'merchant.detach';");
        }
    }
}
