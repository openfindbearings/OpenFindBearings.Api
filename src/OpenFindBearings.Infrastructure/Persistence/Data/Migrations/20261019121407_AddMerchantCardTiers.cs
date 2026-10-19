using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMerchantCardTiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AdoptedAt",
                table: "SourcingResponses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FeedbackAt",
                table: "SourcingResponses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeedbackIp",
                table: "SourcingResponses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FeedbackOutcome",
                table: "SourcingResponses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeedbackReason",
                table: "SourcingResponses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlagNote",
                table: "SourcingResponses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCounted",
                table: "SourcingResponses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContactUnlockedAt",
                table: "SourcingDemands",
                type: "timestamp with time zone",
                nullable: true);

            // 改动说明（v2.18.0 卡级重构·存量数据迁移）：
            // 1) 卡级枚举数值单调化，存量值互换——旧 Verified=3 → 新 Bronze=2；旧 Premium=2 → 新 Silver=3
            //    （Standard=1 与 Gold=4 不变；-1 仅作交换暂存值，非任何合法枚举值）
            // 2) 定级配置键迁移：在售前置合并为单键（旧 Lv3 键改名沿用其值）、删除金库/金牌在售分档键、新增窗口与撮合次数键
            // 幂等说明：系统配置的改名/删除/插入均带存在性守卫，重复执行安全
            migrationBuilder.Sql(@"
UPDATE ""Merchants"" SET ""Grade"" = -1 WHERE ""Grade"" = 2;
UPDATE ""Merchants"" SET ""Grade"" = 2 WHERE ""Grade"" = 3;
UPDATE ""Merchants"" SET ""Grade"" = 3 WHERE ""Grade"" = -1;

UPDATE ""SystemConfigs"" SET ""Key"" = 'Business.MerchantGradeOnSaleMin'
 WHERE ""Key"" = 'Business.MerchantPremiumOnSaleMin'
   AND NOT EXISTS (SELECT 1 FROM ""SystemConfigs"" s WHERE s.""Key"" = 'Business.MerchantGradeOnSaleMin');
DELETE FROM ""SystemConfigs"" WHERE ""Key"" IN ('Business.MerchantPremiumOnSaleMin', 'Business.MerchantGoldOnSaleMin', 'Business.MerchantGoldTreasuryMin');

INSERT INTO ""SystemConfigs"" (""Id"", ""Key"", ""Value"", ""Description"", ""Group"", ""ValueType"", ""IsSystem"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), v.""Key"", v.""Value"", v.""Desc"", 'Business', 'int', true, now(), true
FROM (VALUES
 ('Business.MerchantSilverDealMin', '1', '银卡：近窗口期成功撮合次数下限'),
 ('Business.MerchantGoldDealMin', '3', '金卡：近窗口期成功撮合次数下限'),
 ('Business.MerchantGradeWindowDays', '180', '卡级成功撮合统计滚动窗口天数')
) AS v(""Key"", ""Value"", ""Desc"")
WHERE NOT EXISTS (SELECT 1 FROM ""SystemConfigs"" s WHERE s.""Key"" = v.""Key"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 改动说明（v2.18.0 卡级重构·回滚）：数据侧反转——卡级值换回旧枚举，配置键换回旧三键并清新增键
            migrationBuilder.Sql(@"
UPDATE ""Merchants"" SET ""Grade"" = -1 WHERE ""Grade"" = 3;
UPDATE ""Merchants"" SET ""Grade"" = 3 WHERE ""Grade"" = 2;
UPDATE ""Merchants"" SET ""Grade"" = 2 WHERE ""Grade"" = -1;

DELETE FROM ""SystemConfigs"" WHERE ""Key"" IN ('Business.MerchantGradeOnSaleMin', 'Business.MerchantSilverDealMin', 'Business.MerchantGoldDealMin', 'Business.MerchantGradeWindowDays');

INSERT INTO ""SystemConfigs"" (""Id"", ""Key"", ""Value"", ""Description"", ""Group"", ""ValueType"", ""IsSystem"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), v.""Key"", v.""Value"", v.""Desc"", 'Business', 'int', true, now(), true
FROM (VALUES
 ('Business.MerchantPremiumOnSaleMin', '5', '商家 Lv3 活跃供给：认证+在售达此数'),
 ('Business.MerchantGoldOnSaleMin', '10', '商家 Lv4 金牌：认证+在售达此数'),
 ('Business.MerchantGoldTreasuryMin', '500', '商家 Lv4 金牌：金库累计入账达此数')
) AS v(""Key"", ""Value"", ""Desc"")
WHERE NOT EXISTS (SELECT 1 FROM ""SystemConfigs"" s WHERE s.""Key"" = v.""Key"");");

            migrationBuilder.DropColumn(
                name: "AdoptedAt",
                table: "SourcingResponses");

            migrationBuilder.DropColumn(
                name: "FeedbackAt",
                table: "SourcingResponses");

            migrationBuilder.DropColumn(
                name: "FeedbackIp",
                table: "SourcingResponses");

            migrationBuilder.DropColumn(
                name: "FeedbackOutcome",
                table: "SourcingResponses");

            migrationBuilder.DropColumn(
                name: "FeedbackReason",
                table: "SourcingResponses");

            migrationBuilder.DropColumn(
                name: "FlagNote",
                table: "SourcingResponses");

            migrationBuilder.DropColumn(
                name: "IsCounted",
                table: "SourcingResponses");

            migrationBuilder.DropColumn(
                name: "ContactUnlockedAt",
                table: "SourcingDemands");
        }
    }
}
