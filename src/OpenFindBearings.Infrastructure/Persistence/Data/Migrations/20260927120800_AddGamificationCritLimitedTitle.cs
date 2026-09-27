using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGamificationCritLimitedTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EquippedTitle",
                table: "Users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DoubleChance",
                table: "PointGrantRules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LegendChance",
                table: "PointGrantRules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsLimited",
                table: "AchievementDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LimitedOrdinal",
                table: "AchievementDefinitions",
                type: "integer",
                nullable: true);

            // 改动说明（v2.8.0 G1/G11 种子）：签到规则给暴击概率（10% 双倍 / 2% 传说）；
            // 创站元老限量成就（前 100 注册专属，绝版不返场）——IsLimited=true + LimitedOrdinal=100，
            // 注册序号判定由注册链路调用成就服务按序号解锁（不进 MetricKey 计数引擎）
            migrationBuilder.Sql(@"
                UPDATE ""PointGrantRules"" SET ""DoubleChance"" = 10, ""LegendChance"" = 2, ""UpdatedAt"" = now()
                WHERE ""GrantType"" = 'daily_checkin';

                INSERT INTO ""AchievementDefinitions"" (""Id"", ""Key"", ""Name"", ""Description"", ""Icon"", ""ImageKey"", ""Scope"", ""Category"", ""MetricKey"", ""ProgressTarget"", ""MetaPoints"", ""RewardPoints"", ""TitleReward"", ""Rare"", ""Hidden"", ""IsLimited"", ""LimitedOrdinal"", ""Enabled"", ""CreatedAt"", ""UpdatedAt"", ""IsActive"")
                SELECT gen_random_uuid(), 'founder_elder', '创站元老', '平台前 100 位注册用户专属徽章，窗口已关闭，绝版不返场', 'medal', NULL, 1, '元老', 'founder_ordinal', 1, 60, 0, '创站元老', true, true, true, 100, true, now(), now(), true
                WHERE NOT EXISTS (SELECT 1 FROM ""AchievementDefinitions"" WHERE ""Key"" = 'founder_elder');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EquippedTitle",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DoubleChance",
                table: "PointGrantRules");

            migrationBuilder.DropColumn(
                name: "LegendChance",
                table: "PointGrantRules");

            migrationBuilder.DropColumn(
                name: "IsLimited",
                table: "AchievementDefinitions");

            migrationBuilder.DropColumn(
                name: "LimitedOrdinal",
                table: "AchievementDefinitions");
        }
    }
}
