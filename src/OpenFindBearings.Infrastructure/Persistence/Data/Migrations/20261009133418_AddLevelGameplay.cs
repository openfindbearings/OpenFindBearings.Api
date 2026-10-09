using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLevelGameplay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RewardPoints",
                table: "AchievementDefinitions");

            migrationBuilder.AddColumn<int>(
                name: "LevelUpBonus",
                table: "PointLevels",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "GradeGraceUntil",
                table: "Merchants",
                type: "timestamp with time zone",
                nullable: true);

            // 改动说明（v2.12.0 等级玩法）：以下数据操作全部幂等——
            // 段位改名（武侠风十档 → 王者风段位，Admin 档位表可再自定义）、
            // 升档礼回填（Lv1 恒 0，Lv2~10 递增 10~120）、level_up_bonus 规则种子、
            // achievement_unlock 规则停用（成就纯荣誉化，历史流水保留）、
            // 商家升档礼/保级期 Business 配置种子（与 AddMerchantTreasury 同款列清单防启动 CrashLoop）
            migrationBuilder.Sql(@"
UPDATE ""PointLevels"" p SET ""Name"" = v.n FROM (VALUES
 (1, '倔强青铜'),
 (2, '坚韧青铜'),
 (3, '秩序白银'),
 (4, '璀璨白银'),
 (5, '荣耀黄金'),
 (6, '华彩铂金'),
 (7, '永恒钻石'),
 (8, '至尊星耀'),
 (9, '傲世星耀'),
 (10, '最强王者')
) AS v(lv, n)
WHERE p.""Level"" = v.lv;");

            migrationBuilder.Sql(@"
UPDATE ""PointLevels"" p SET ""LevelUpBonus"" = v.b FROM (VALUES
 (1, 0),
 (2, 10),
 (3, 15),
 (4, 20),
 (5, 30),
 (6, 40),
 (7, 55),
 (8, 70),
 (9, 90),
 (10, 120)
) AS v(lv, b)
WHERE p.""Level"" = v.lv;");

            migrationBuilder.Sql(@"
INSERT INTO ""PointGrantRules"" (""Id"", ""GrantType"", ""DisplayName"", ""Amount"", ""DailyLimit"", ""LadderJson"", ""IsEnabled"", ""Description"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), 'level_up_bonus', '段位升档礼', 10, 0, NULL, true, '累计获得轴承币跨入新段位时一次性发放（金额按档位表 LevelUpBonus 覆盖，每用户每档终身一次）', now(), true
WHERE NOT EXISTS (SELECT 1 FROM ""PointGrantRules"" r WHERE r.""GrantType"" = 'level_up_bonus');");

            migrationBuilder.Sql(@"
UPDATE ""PointGrantRules"" SET ""IsEnabled"" = false WHERE ""GrantType"" = 'achievement_unlock';");

            migrationBuilder.Sql(@"
INSERT INTO ""SystemConfigs"" (""Id"", ""Key"", ""Value"", ""Description"", ""Group"", ""ValueType"", ""IsSystem"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), v.""Key"", v.""Value"", v.""Desc"", 'Business', 'int', true, now(), true
FROM (VALUES
 ('Business.MerchantGradeGraceDays', '15', '商家等级降档保级缓冲天数（重算不达标后维持原档的宽限期；0=立即降档）'),
 ('Business.MerchantGradeBonusLv2', '50', '商家升到 Lv2 认证的一次性升档礼（商家金入金库，每商户每档终身一次）'),
 ('Business.MerchantGradeBonusLv3', '150', '商家升到 Lv3 活跃供给的一次性升档礼（商家金入金库，每商户每档终身一次）'),
 ('Business.MerchantGradeBonusLv4', '300', '商家升到 Lv4 金牌的一次性升档礼（商家金入金库，每商户每档终身一次）')
) AS v(""Key"", ""Value"", ""Desc"")
WHERE NOT EXISTS (SELECT 1 FROM ""SystemConfigs"" s WHERE s.""Key"" = v.""Key"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LevelUpBonus",
                table: "PointLevels");

            migrationBuilder.DropColumn(
                name: "GradeGraceUntil",
                table: "Merchants");

            migrationBuilder.AddColumn<int>(
                name: "RewardPoints",
                table: "AchievementDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
