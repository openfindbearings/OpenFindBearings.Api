using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPointSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PointAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Balance = table.Column<int>(type: "integer", nullable: false),
                    TotalEarned = table.Column<int>(type: "integer", nullable: false),
                    TotalSpent = table.Column<int>(type: "integer", nullable: false),
                    LastCheckinDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConsecutiveCheckinDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PointGrantRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    DailyLimit = table.Column<int>(type: "integer", nullable: false),
                    LadderJson = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointGrantRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PointTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    GrantType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    BalanceAfter = table.Column<int>(type: "integer", nullable: false),
                    BizId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Remark = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExpireAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointTransactions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PointAccounts_UserId",
                table: "PointAccounts",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PointGrantRules_GrantType",
                table: "PointGrantRules",
                column: "GrantType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PointTransactions_UserId_CreatedAt",
                table: "PointTransactions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PointTransactions_UserId_GrantType_CreatedAt",
                table: "PointTransactions",
                columns: new[] { "UserId", "GrantType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_PointTransactions_BizId",
                table: "PointTransactions",
                column: "BizId",
                unique: true,
                filter: "\"BizId\" IS NOT NULL");

            // 改动说明（v1.32.0）：默认赚分规则种子走迁移而非 SeedData——存量库
            //   SeedData 按"Users 非空"整体短路永不执行，迁移是唯一可靠落点；幂等 NOT EXISTS
            migrationBuilder.Sql(@"
INSERT INTO ""PointGrantRules"" (""Id"", ""GrantType"", ""DisplayName"", ""Amount"", ""DailyLimit"", ""LadderJson"", ""IsEnabled"", ""Description"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), 'daily_login', '每日登录', 1, 0, NULL, true, '当日首次业务请求自动发放（被动保底）', now(), true
WHERE NOT EXISTS (SELECT 1 FROM ""PointGrantRules"" WHERE ""GrantType"" = 'daily_login');
INSERT INTO ""PointGrantRules"" (""Id"", ""GrantType"", ""DisplayName"", ""Amount"", ""DailyLimit"", ""LadderJson"", ""IsEnabled"", ""Description"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), 'daily_checkin', '每日签到', 2, 0, '[2,3,4,5,5]', true, '主动签到，连续天数阶梯（1/2/3/4/5+天），超末档取末档', now(), true
WHERE NOT EXISTS (SELECT 1 FROM ""PointGrantRules"" WHERE ""GrantType"" = 'daily_checkin');
INSERT INTO ""PointGrantRules"" (""Id"", ""GrantType"", ""DisplayName"", ""Amount"", ""DailyLimit"", ""LadderJson"", ""IsEnabled"", ""Description"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), 'register_bonus', '新用户注册奖励', 50, 0, NULL, true, '首次创建业务用户一次性发放（冷启动钩子）', now(), true
WHERE NOT EXISTS (SELECT 1 FROM ""PointGrantRules"" WHERE ""GrantType"" = 'register_bonus');
INSERT INTO ""PointGrantRules"" (""Id"", ""GrantType"", ""DisplayName"", ""Amount"", ""DailyLimit"", ""LadderJson"", ""IsEnabled"", ""Description"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), 'correction_adopted', '', 纠错被采纳', 20, 100, NULL, true, '纠错审核采纳发放，每日上限 100 防灌水', now(), true
WHERE NOT EXISTS (SELECT 1 FROM ""PointGrantRules"" WHERE ""GrantType"" = 'correction_adopted');
INSERT INTO ""PointGrantRules"" (""Id"", ""GrantType"", ""DisplayName"", ""Amount"", ""DailyLimit"", ""LadderJson"", ""IsEnabled"", ""Description"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), 'merchant_approved', '商户入驻通过', 100, 0, NULL, true, '入驻审核通过一次性发放（供给侧最高价值动作）', now(), true
WHERE NOT EXISTS (SELECT 1 FROM ""PointGrantRules"" WHERE ""GrantType"" = 'merchant_approved');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PointAccounts");

            migrationBuilder.DropTable(
                name: "PointGrantRules");

            migrationBuilder.DropTable(
                name: "PointTransactions");
        }
    }
}
