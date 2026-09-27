using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMerchantTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MerchantTaskCompletions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MerchantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodKey = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    MetricValue = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MerchantTaskCompletions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MerchantTaskDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MetricKey = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetValue = table.Column<int>(type: "integer", nullable: false),
                    Period = table.Column<int>(type: "integer", nullable: false),
                    RewardType = table.Column<int>(type: "integer", nullable: false),
                    RewardAmount = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MerchantTaskDefinitions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_MerchantTaskCompletions_Task_Merchant_Period",
                table: "MerchantTaskCompletions",
                columns: new[] { "TaskKey", "MerchantId", "PeriodKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_MerchantTaskDefinitions_Key",
                table: "MerchantTaskDefinitions",
                column: "TaskKey",
                unique: true);

            // v2.6.0 M3 种子：三条开箱任务（周纠错/周上新/月金库）+ 成员奖励发放规则
            // 幂等写法与 AddPointSystem/AddAchievements 种子同款；阈值 Admin 可实时调
            migrationBuilder.Sql(@"
INSERT INTO ""MerchantTaskDefinitions"" (""Id"", ""TaskKey"", ""Name"", ""Description"", ""MetricKey"", ""TargetValue"", ""Period"", ""RewardType"", ""RewardAmount"", ""Enabled"", ""SortOrder"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), 'merchant_corrections_week', '全员纠错周', '本自然周内成员纠错被采纳合计达标，全体在职成员各得积分', 'corrections', 5, 1, 1, 20, true, 1, now(), true
WHERE NOT EXISTS (SELECT 1 FROM ""MerchantTaskDefinitions"" WHERE ""TaskKey"" = 'merchant_corrections_week');
INSERT INTO ""MerchantTaskDefinitions"" (""Id"", ""TaskKey"", ""Name"", ""Description"", ""MetricKey"", ""TargetValue"", ""Period"", ""RewardType"", ""RewardAmount"", ""Enabled"", ""SortOrder"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), 'merchant_products_week', '全员上新周', '本自然周内新上架商品合计达标，商家金库获得推广奖励', 'products', 5, 1, 2, 100, true, 2, now(), true
WHERE NOT EXISTS (SELECT 1 FROM ""MerchantTaskDefinitions"" WHERE ""TaskKey"" = 'merchant_products_week');
INSERT INTO ""MerchantTaskDefinitions"" (""Id"", ""TaskKey"", ""Name"", ""Description"", ""MetricKey"", ""TargetValue"", ""Period"", ""RewardType"", ""RewardAmount"", ""Enabled"", ""SortOrder"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), 'merchant_treasury_month', '金库冲刺月', '本自然月金库入账合计达标，全体在职成员各得积分', 'treasury', 300, 2, 1, 50, true, 3, now(), true
WHERE NOT EXISTS (SELECT 1 FROM ""MerchantTaskDefinitions"" WHERE ""TaskKey"" = 'merchant_treasury_month');");

            // merchant_task 发放规则：分值由任务定义 amountOverride 覆盖（同 achievement_unlock 模式），DailyLimit=0 不限
            migrationBuilder.Sql(@"
INSERT INTO ""PointGrantRules"" (""Id"", ""GrantType"", ""DisplayName"", ""Amount"", ""DailyLimit"", ""LadderJson"", ""IsEnabled"", ""Description"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), 'merchant_task', '商家集体任务奖励', 20, 0, NULL, true, '商家集体任务达标结算发放（分值按任务定义覆盖，Job 自动发放）', now(), true
WHERE NOT EXISTS (SELECT 1 FROM ""PointGrantRules"" WHERE ""GrantType"" IN ('merchant_task', 'guild_task'));");

            // 术语统一自愈（幂等）：曾经应用过旧 AddGuildTasks 迁移的环境清掉 Guild* 孤儿表与历史行；
            // 曾执行过旧版 AddMerchantTreasury 种子的环境把 Business.Guild* 三键改名搬运到 Merchant*（保 Admin 已调值）
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS ""GuildTaskCompletions"";
DROP TABLE IF EXISTS ""GuildTaskDefinitions"";
DELETE FROM ""__EFMigrationsHistory"" WHERE ""MigrationId"" = '20260927023524_AddGuildTasks';
UPDATE ""SystemConfigs"" SET ""Key"" = 'Business.MerchantPremiumOnSaleMin'
 WHERE ""Key"" = 'Business.GuildPremiumOnSaleMin'
   AND NOT EXISTS (SELECT 1 FROM ""SystemConfigs"" s WHERE s.""Key"" = 'Business.MerchantPremiumOnSaleMin');
UPDATE ""SystemConfigs"" SET ""Key"" = 'Business.MerchantGoldOnSaleMin'
 WHERE ""Key"" = 'Business.GuildGoldOnSaleMin'
   AND NOT EXISTS (SELECT 1 FROM ""SystemConfigs"" s WHERE s.""Key"" = 'Business.MerchantGoldOnSaleMin');
UPDATE ""SystemConfigs"" SET ""Key"" = 'Business.MerchantGoldTreasuryMin'
 WHERE ""Key"" = 'Business.GuildGoldTreasuryMin'
   AND NOT EXISTS (SELECT 1 FROM ""SystemConfigs"" s WHERE s.""Key"" = 'Business.MerchantGoldTreasuryMin');
DELETE FROM ""SystemConfigs"" WHERE ""Key"" IN ('Business.GuildPremiumOnSaleMin', 'Business.GuildGoldOnSaleMin', 'Business.GuildGoldTreasuryMin');
UPDATE ""PointGrantRules"" SET ""GrantType"" = 'merchant_task', ""DisplayName"" = '商家集体任务奖励', ""Description"" = '商家集体任务达标结算发放（分值按任务定义覆盖，Job 自动发放）'
 WHERE ""GrantType"" = 'guild_task';");

            // v2.6.0 新手旅程成就：低门槛勋章让新用户首日即可点亮（勋章卡冷启动问题）；
            // register_total/login_total/sourcing_publish_total 为本批新挂计数键，其余复用既有键。
            // 幂等写法与 AddAchievements 种子同款
            migrationBuilder.Sql(@"
INSERT INTO ""AchievementDefinitions"" (""Id"",""Key"",""Name"",""Description"",""Icon"",""Scope"",""Category"",""MetricKey"",""ProgressTarget"",""MetaPoints"",""RewardPoints"",""TitleReward"",""Rare"",""Hidden"",""Enabled"",""CreatedAt"",""IsActive"")
SELECT gen_random_uuid(), v.""Key"", v.""Name"", v.""Description"", v.""Icon"", v.""Scope"", v.""Category"", v.""MetricKey"", v.""Target"", v.""Meta"", v.""Reward"", v.""Title"", v.""Rare"", v.""Hidden"", true, now(), true
FROM (VALUES
 ('checkin_first','初次签到','完成首次每日签到','check',1,'新手旅程','checkin_total',1,5,5,NULL,false,false),
 ('checkin_3','三日之约','连续签到 3 天','calendar',1,'新手旅程','checkin_streak',3,10,5,NULL,false,false),
 ('checkin_total_10','签到集锦','累计签到 10 次','calendar-check',1,'新手旅程','checkin_total',10,25,10,NULL,false,false),
 ('register_first','初来乍到','注册加入平台','sparkles',1,'新手旅程','register_total',1,5,5,NULL,false,false),
 ('login_7','七日陪伴','累计登录 7 天','flame',1,'新手旅程','login_total',7,20,10,NULL,false,false),
 ('sourcing_first','旗开得胜','发布首条寻货需求','flag',1,'新手旅程','sourcing_publish_total',1,15,5,NULL,false,false)
) AS v(""Key"",""Name"",""Description"",""Icon"",""Scope"",""Category"",""MetricKey"",""Target"",""Meta"",""Reward"",""Title"",""Rare"",""Hidden"")
WHERE NOT EXISTS (SELECT 1 FROM ""AchievementDefinitions"" a WHERE a.""Key"" = v.""Key"");");

            // 术语统一（成就/徽章→勋章）：解锁奖励规则的用户可见名/说明搬运（幂等，新旧库一致收敛）
            migrationBuilder.Sql(@"
UPDATE ""PointGrantRules"" SET ""DisplayName"" = '勋章解锁奖励', ""Description"" = '勋章点亮一次性可花积分甜头（实际分值按勋章定义覆盖）'
 WHERE ""GrantType"" = 'achievement_unlock';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MerchantTaskCompletions");

            migrationBuilder.DropTable(
                name: "MerchantTaskDefinitions");

            migrationBuilder.Sql(@"DELETE FROM ""PointGrantRules"" WHERE ""GrantType"" = 'merchant_task';");
        }
    }
}
