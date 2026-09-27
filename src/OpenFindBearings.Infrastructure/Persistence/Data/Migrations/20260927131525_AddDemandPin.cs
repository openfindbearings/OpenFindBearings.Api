using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDemandPin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PinnedUntil",
                table: "SourcingDemands",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetKind",
                table: "MallItems",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // 改动说明（v2.10.0 寻货置顶 + 商家金）：
            // 1) 新增两档"寻货置顶卡"（TargetKind=2，个人积分定价，无金库代付）
            // 2) 存量"置顶卡"改名"商品置顶卡"（描述点明商家金定价，幂等自愈）
            // 3) 个人代付商品置顶的折算汇率配置键（1 商家金=N 积分，默认 2）
            migrationBuilder.Sql(@"
INSERT INTO ""MallItems"" (""Id"", ""Key"", ""Name"", ""Description"", ""Icon"", ""Category"", ""PointPrice"", ""DurationHours"", ""Stock"", ""SoldCount"", ""Enabled"", ""SortOrder"", ""TargetKind"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), v.""Key"", v.""Name"", v.""Description"", v.""Icon"", 1, v.""PointPrice"", v.""DurationHours"", -1, 0, true, v.""SortOrder"", 2, (now() at time zone 'utc'), true
FROM (VALUES
    ('demand_pin_24h', '24小时寻货置顶卡', '把你发布的一条寻货需求在公开大厅置顶 24 小时，商家优先看到（个人积分支付）', 'pin', 60, 24, 30),
    ('demand_pin_72h', '72小时寻货置顶卡', '把你发布的一条寻货需求在公开大厅置顶 72 小时，曝光拉满（个人积分支付）', 'rocket', 160, 72, 40)
) AS v(""Key"", ""Name"", ""Description"", ""Icon"", ""PointPrice"", ""DurationHours"", ""SortOrder"")
WHERE NOT EXISTS (SELECT 1 FROM ""MallItems"" m WHERE m.""Key"" = v.""Key"");");

            migrationBuilder.Sql(@"
UPDATE ""MallItems"" SET ""Name"" = '24小时商品置顶卡',
    ""Description"" = '让一个在售商品在该型号商家列表置顶 24 小时（商家金定价，管理员金库支付；个人代付按汇率折算）',
    ""TargetKind"" = 1, ""UpdatedAt"" = (now() at time zone 'utc')
WHERE ""Key"" = 'pin_24h' AND ""Name"" = '24小时置顶卡';
UPDATE ""MallItems"" SET ""Name"" = '72小时商品置顶卡',
    ""Description"" = '让一个在售商品在该型号商家列表置顶 72 小时（商家金定价，管理员金库支付；个人代付按汇率折算）',
    ""TargetKind"" = 1, ""UpdatedAt"" = (now() at time zone 'utc')
WHERE ""Key"" = 'pin_72h' AND ""Name"" = '72小时置顶卡';");

            migrationBuilder.Sql(@"
INSERT INTO ""SystemConfigs"" (""Id"", ""Group"", ""Key"", ""Value"", ""Description"", ""ValueType"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), 'Business', 'Business.MerchantGoldPayRate', '2', '商品置顶个人代付汇率：1 商家金折算 N 个人积分（仅商家管理员个人通道生效，金库通道原价）', 'int', (now() at time zone 'utc'), true
WHERE NOT EXISTS (SELECT 1 FROM ""SystemConfigs"" c WHERE c.""Key"" = 'Business.MerchantGoldPayRate');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PinnedUntil",
                table: "SourcingDemands");

            migrationBuilder.DropColumn(
                name: "TargetKind",
                table: "MallItems");
        }
    }
}
