using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <summary>
    /// v2.18.0 卡级重构：新增"调整商家卡级"独立权限键（人工调级 + 回执计分撤销），
    /// 并补插成交回执反刷三条规则配置（存量库 SeedData 整体短路，新增键/配置必须走迁移）。
    /// </summary>
    /// <inheritdoc />
    public partial class AddGradeManagePermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 权限键 merchant.grade.manage：Admin 全量、Operator（日常审核）给权、Auditor 只读不给。
            // 角色授权用子查询关联角色名，不硬编码 Guid；幂等 NOT EXISTS。
            migrationBuilder.Sql(@"
INSERT INTO ""Permissions"" (""Id"", ""Name"", ""Description"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), v.n, v.d, now(), true
FROM (VALUES
    ('merchant.grade.manage', '调整商家卡级')
) AS v(n, d)
WHERE NOT EXISTS (SELECT 1 FROM ""Permissions"" WHERE ""Name"" = v.n);

INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"", ""Id"", ""CreatedAt"", ""IsActive"")
SELECT r.""Id"", p.""Id"", gen_random_uuid(), now(), true
FROM ""Roles"" r, ""Permissions"" p
WHERE ((r.""Name"" = 'Admin' AND p.""Name"" = 'merchant.grade.manage')
    OR (r.""Name"" = 'Operator' AND p.""Name"" = 'merchant.grade.manage'))
  AND NOT EXISTS (
    SELECT 1 FROM ""RolePermissions"" rp
    WHERE rp.""RoleId"" = r.""Id"" AND rp.""PermissionId"" = p.""Id""
  );

INSERT INTO ""SystemConfigs"" (""Id"", ""Key"", ""Value"", ""Description"", ""Group"", ""ValueType"", ""IsSystem"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), v.""Key"", v.""Value"", v.""Desc"", 'Business', 'int', true, now(), true
FROM (VALUES
 ('Business.SourcingFeedbackMinAccountAgeDays', '1', '成交回执计分门槛：需求方账号最短注册天数（0=不校验）'),
 ('Business.SourcingFeedbackIpWindowHours', '24', '成交回执反刷：同 IP 异常检测时间窗（小时）'),
 ('Business.SourcingFeedbackIpMax', '5', '成交回执反刷：同 IP 时间窗内允许的计分回执上限')
) AS v(""Key"", ""Value"", ""Desc"")
WHERE NOT EXISTS (SELECT 1 FROM ""SystemConfigs"" s WHERE s.""Key"" = v.""Key"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 回滚：先删角色授权再删权限键；同时清理本次新增的三条回执规则配置
            migrationBuilder.Sql(@"
DELETE FROM ""RolePermissions"" WHERE ""PermissionId"" IN (SELECT ""Id"" FROM ""Permissions"" WHERE ""Name"" = 'merchant.grade.manage');
DELETE FROM ""Permissions"" WHERE ""Name"" = 'merchant.grade.manage';
DELETE FROM ""SystemConfigs"" WHERE ""Key"" IN ('Business.SourcingFeedbackMinAccountAgeDays', 'Business.SourcingFeedbackIpWindowHours', 'Business.SourcingFeedbackIpMax');");
        }
    }
}
