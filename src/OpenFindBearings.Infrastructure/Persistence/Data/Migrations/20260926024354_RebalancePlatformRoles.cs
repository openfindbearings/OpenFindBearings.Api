using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class RebalancePlatformRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // v1.31.0 平台角色重整（存量库）：
            // 一、删除 MerchantAdmin/MerchantStaff 僵尸角色——商户鉴权只信 MerchantMembers 成员表，
            //    Roles 表同名角色永无赋值与消费方（先删角色权限映射再删角色；UserRoles 无引用）；
            // 二、补 sync.review 权限点并授予 Admin（Admin=全权语义）；
            // 三、补 Operator/Auditor 角色落地轻量三员分立（操作员=日常 CRUD+审核、审计员=只读+审计），
            //    权限按名称清单授予，全部幂等（NOT EXISTS 防重复执行）。
            migrationBuilder.Sql(@"
DELETE FROM ""RolePermissions"" WHERE ""RoleId"" IN (SELECT ""Id"" FROM ""Roles"" WHERE ""Name"" IN ('MerchantAdmin','MerchantStaff'));
DELETE FROM ""Roles"" WHERE ""Name"" IN ('MerchantAdmin','MerchantStaff');

INSERT INTO ""Permissions"" (""Id"", ""Name"", ""Description"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), 'sync.review', '同步数据审核', now(), true
WHERE NOT EXISTS (SELECT 1 FROM ""Permissions"" WHERE ""Name"" = 'sync.review');

INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"", ""Id"", ""CreatedAt"", ""IsActive"")
SELECT r.""Id"", p.""Id"", gen_random_uuid(), now(), true
FROM ""Roles"" r, ""Permissions"" p
WHERE r.""Name"" = 'Admin' AND p.""Name"" = 'sync.review'
  AND NOT EXISTS (SELECT 1 FROM ""RolePermissions"" rp WHERE rp.""RoleId"" = r.""Id"" AND rp.""PermissionId"" = p.""Id"");

INSERT INTO ""Roles"" (""Id"", ""Name"", ""Description"", ""IsSystem"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), 'Operator', '操作员', true, now(), true
WHERE NOT EXISTS (SELECT 1 FROM ""Roles"" WHERE ""Name"" = 'Operator');
INSERT INTO ""Roles"" (""Id"", ""Name"", ""Description"", ""IsSystem"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), 'Auditor', '审计员', true, now(), true
WHERE NOT EXISTS (SELECT 1 FROM ""Roles"" WHERE ""Name"" = 'Auditor');

INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"", ""Id"", ""CreatedAt"", ""IsActive"")
SELECT r.""Id"", p.""Id"", gen_random_uuid(), now(), true
FROM ""Roles"" r, ""Permissions"" p
WHERE r.""Name"" = 'Operator'
  AND p.""Name"" IN ('bearing.view','bearing.create','bearing.edit','merchant.view','merchant.verify','correction.submit','correction.review','sync.review','dashboard.view','data.restore')
  AND NOT EXISTS (SELECT 1 FROM ""RolePermissions"" rp WHERE rp.""RoleId"" = r.""Id"" AND rp.""PermissionId"" = p.""Id"");

INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"", ""Id"", ""CreatedAt"", ""IsActive"")
SELECT r.""Id"", p.""Id"", gen_random_uuid(), now(), true
FROM ""Roles"" r, ""Permissions"" p
WHERE r.""Name"" = 'Auditor'
  AND p.""Name"" IN ('dashboard.view','audit.view','system.view','bearing.view','merchant.view','correction.review','sync.review')
  AND NOT EXISTS (SELECT 1 FROM ""RolePermissions"" rp WHERE rp.""RoleId"" = r.""Id"" AND rp.""PermissionId"" = p.""Id"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM ""RolePermissions"" WHERE ""RoleId"" IN (SELECT ""Id"" FROM ""Roles"" WHERE ""Name"" IN ('Operator','Auditor'));
DELETE FROM ""Roles"" WHERE ""Name"" IN ('Operator','Auditor');
DELETE FROM ""RolePermissions"" WHERE ""PermissionId"" IN (SELECT ""Id"" FROM ""Permissions"" WHERE ""Name"" = 'sync.review');
DELETE FROM ""Permissions"" WHERE ""Name"" = 'sync.review';");
        }
    }
}
