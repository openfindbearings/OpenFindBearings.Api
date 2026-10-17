using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <summary>
    /// 开源去 Sync 影子（v2.18.0 通用扩展机制）：
    /// 1) 系统配置分组 Sync 更名为 System；
    /// 2) 删除 sync.run/sync.review 权限及其角色绑定（Sync 专属，改由扩展补种）。
    /// 数据迁移，不改表结构，模型快照不变。
    /// </summary>
    public partial class RemoveSyncShadow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE ""SystemConfigs"" SET ""Group"" = 'System' WHERE ""Group"" = 'Sync';

DELETE FROM ""RolePermissions"" WHERE ""PermissionId"" IN
    (SELECT ""Id"" FROM ""Permissions"" WHERE ""Name"" IN ('sync.run','sync.review','review.sync'));
DELETE FROM ""Permissions"" WHERE ""Name"" IN ('sync.run','sync.review','review.sync');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE ""SystemConfigs"" SET ""Group"" = 'Sync' WHERE ""Group"" = 'System';

INSERT INTO ""Permissions"" (""Id"", ""Name"", ""Description"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), 'sync.review', '同步数据审核', now(), true
WHERE NOT EXISTS (SELECT 1 FROM ""Permissions"" WHERE ""Name"" = 'sync.review');
INSERT INTO ""Permissions"" (""Id"", ""Name"", ""Description"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), 'sync.run', '触发同步任务', now(), true
WHERE NOT EXISTS (SELECT 1 FROM ""Permissions"" WHERE ""Name"" = 'sync.run');

INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"", ""Id"", ""CreatedAt"", ""IsActive"")
SELECT r.""Id"", p.""Id"", gen_random_uuid(), now(), true
FROM ""Roles"" r, ""Permissions"" p
WHERE r.""Name"" = 'Admin' AND p.""Name"" IN ('sync.review','sync.run')
  AND NOT EXISTS (SELECT 1 FROM ""RolePermissions"" rp WHERE rp.""RoleId"" = r.""Id"" AND rp.""PermissionId"" = p.""Id"");
");
        }
    }
}