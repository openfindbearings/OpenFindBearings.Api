using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyComboAndPointLevels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PointLevels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    MinTotalEarned = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointLevels", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_PointLevels_Level",
                table: "PointLevels",
                column: "Level",
                unique: true);

            // 改动说明（v2.7.0 G2/G7 种子）：daily_combo 三件套规则行（30 分/日上限 1）+
            // 用户积分等级阈值 10 档（G7 纯展示）。全部幂等（NOT EXISTS），已发布库补种不重复。
            migrationBuilder.Sql(@"
                INSERT INTO ""PointGrantRules"" (""Id"", ""GrantType"", ""DisplayName"", ""Amount"", ""DailyLimit"", ""LadderJson"", ""IsEnabled"", ""Description"", ""CreatedAt"", ""IsActive"")
                SELECT gen_random_uuid(), 'daily_combo', '每日任务板三件套', 30, 1, NULL, true, '今日 签到 + 纠错被采纳 + 寻货应答 三项全完成后额外 +30 分', now(), true
                WHERE NOT EXISTS (SELECT 1 FROM ""PointGrantRules"" r WHERE r.""GrantType"" = 'daily_combo');

                INSERT INTO ""PointLevels"" (""Id"", ""Level"", ""MinTotalEarned"", ""Name"", ""Enabled"", ""CreatedAt"", ""UpdatedAt"", ""IsActive"")
                SELECT gen_random_uuid(), v.lv, v.min, v.n, true, now(), now(), true
                FROM (VALUES
                    (1, 0,    '初出茅庐'),
                    (2, 100,  '小有所成'),
                    (3, 300,  '崭露头角'),
                    (4, 600,  '渐入佳境'),
                    (5, 1000, '炉火纯青'),
                    (6, 1600, '行家里手'),
                    (7, 2400, '登堂入室'),
                    (8, 3400, '出类拔萃'),
                    (9, 4600, '登峰造极'),
                    (10, 6000, '独孤求败')
                ) AS v(lv, min, n)
                WHERE NOT EXISTS (SELECT 1 FROM ""PointLevels"" p WHERE p.""Level"" = v.lv);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_PointLevels_Level",
                table: "PointLevels");

            migrationBuilder.DropTable(
                name: "PointLevels");
        }
    }
}
