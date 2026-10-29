using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <summary>
    /// v2.22.1 意见反馈：新增 UserFeedbacks 表，并补插反馈管理权限键
    /// （feedback.view 查看 / feedback.edit 处理与回复）与每日提交上限配置。
    /// 存量库 SeedData 整体短路，新增权限键与配置必须走迁移补齐。
    /// </summary>
    /// <inheritdoc />
    public partial class AddUserFeedbacks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserFeedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ImageUrls = table.Column<string[]>(type: "text[]", nullable: false, defaultValue: new string[0]),
                    Contact = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ClientInfo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AdminReply = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RepliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RepliedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFeedbacks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserFeedbacks_Status_CreatedAt",
                table: "UserFeedbacks",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserFeedbacks_UserId_CreatedAt",
                table: "UserFeedbacks",
                columns: new[] { "UserId", "CreatedAt" });

            // 权限键：Admin 全量、Operator（日常客服处置）给 view + edit、Auditor 只读给 view。
            // 角色授权用子查询关联角色名，不硬编码 Guid；幂等 NOT EXISTS。
            migrationBuilder.Sql(@"
INSERT INTO ""Permissions"" (""Id"", ""Name"", ""Description"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), v.n, v.d, now(), true
FROM (VALUES
    ('feedback.view', '查看意见反馈'),
    ('feedback.edit', '处理与回复意见反馈')
) AS v(n, d)
WHERE NOT EXISTS (SELECT 1 FROM ""Permissions"" WHERE ""Name"" = v.n);

INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"", ""Id"", ""CreatedAt"", ""IsActive"")
SELECT r.""Id"", p.""Id"", gen_random_uuid(), now(), true
FROM ""Roles"" r, ""Permissions"" p
WHERE (
        (r.""Name"" = 'Admin' AND p.""Name"" IN ('feedback.view', 'feedback.edit'))
     OR (r.""Name"" = 'Operator' AND p.""Name"" IN ('feedback.view', 'feedback.edit'))
     OR (r.""Name"" = 'Auditor' AND p.""Name"" = 'feedback.view')
      )
  AND NOT EXISTS (
    SELECT 1 FROM ""RolePermissions"" rp
    WHERE rp.""RoleId"" = r.""Id"" AND rp.""PermissionId"" = p.""Id""
  );

INSERT INTO ""SystemConfigs"" (""Id"", ""Key"", ""Value"", ""Description"", ""Group"", ""ValueType"", ""IsSystem"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), v.""Key"", v.""Value"", v.""Desc"", 'Business', 'int', true, now(), true
FROM (VALUES
 ('Business.FeedbackDailyLimit', '5', '意见反馈每日提交上限（按业务日界计算，0=不限制）')
) AS v(""Key"", ""Value"", ""Desc"")
WHERE NOT EXISTS (SELECT 1 FROM ""SystemConfigs"" s WHERE s.""Key"" = v.""Key"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserFeedbacks");

            // 回滚：先删角色授权再删权限键，并清理反馈每日上限配置
            migrationBuilder.Sql(@"
DELETE FROM ""RolePermissions"" WHERE ""PermissionId"" IN (SELECT ""Id"" FROM ""Permissions"" WHERE ""Name"" IN ('feedback.view', 'feedback.edit'));
DELETE FROM ""Permissions"" WHERE ""Name"" IN ('feedback.view', 'feedback.edit');
DELETE FROM ""SystemConfigs"" WHERE ""Key"" = 'Business.FeedbackDailyLimit';");
        }
    }
}
