using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMerchantTreasury : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedAt",
                table: "MallOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiverAddress",
                table: "MallOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiverName",
                table: "MallOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiverPhone",
                table: "MallOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShipStatus",
                table: "MallOrders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ShipTracking",
                table: "MallOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShippedAt",
                table: "MallOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                table: "MallItems",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AddColumn<string>(
                name: "AuditRemark",
                table: "MallItems",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AuditState",
                table: "MallItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerMerchantId",
                table: "MallItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MerchantPointAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Balance = table.Column<int>(type: "integer", nullable: false),
                    TotalEarned = table.Column<int>(type: "integer", nullable: false),
                    TotalSpent = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MerchantPointAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MerchantPointTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    GrantType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    BalanceAfter = table.Column<int>(type: "integer", nullable: false),
                    BizId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Remark = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MerchantPointTransactions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MallItems_Owner_Audit",
                table: "MallItems",
                columns: new[] { "OwnerMerchantId", "AuditState" });

            // 改动说明（v2.4.0 工会经济）：金库六参数走迁移种子（存量库 SeedData 短路不执行，
            // 与 MallItems 目录种子同理）；幂等 NOT EXISTS，列清单含全部 NOT NULL 列（Id/CreatedAt/IsActive）
            // 防踩坑：漏列必致启动迁移 CrashLoopBackOff
            migrationBuilder.Sql(@"
INSERT INTO ""SystemConfigs"" (""Id"", ""Key"", ""Value"", ""Description"", ""Group"", ""ValueType"", ""IsSystem"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), v.""Key"", v.""Value"", v.""Desc"", 'Business', 'int', true, now(), true
FROM (VALUES
 ('Business.TricklePercent', '10', '成员合格赚分上供商户金库的比例（百分比，0=关闭）'),
 ('Business.TrickleDailyCapPerMerchant', '50', '单商户金库每日 trickle 入账上限'),
 ('Business.TrickleMonthlyCapPerMerchant', '1000', '单商户金库每月 trickle 入账上限'),
 ('Business.GiftSettleMonthlyCap', '2000', '单商户金库每月挂礼结算上限'),
 ('Business.GiftReceiverMonthlyLimit', '3', '同一收货电话/地址每月礼品兑换单数上限（防刷闸）'),
 ('Business.GiftPriceCeiling', '3000', '商家礼品平台定档单价封顶（防定向积分转移）')
) AS v(""Key"", ""Value"", ""Desc"")
WHERE NOT EXISTS (SELECT 1 FROM ""SystemConfigs"" s WHERE s.""Key"" = v.""Key"");");

            migrationBuilder.CreateIndex(
                name: "UX_MerchantPointAccounts_MerchantId",
                table: "MerchantPointAccounts",
                column: "MerchantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MerchantPointTx_Merchant_Created",
                table: "MerchantPointTransactions",
                columns: new[] { "MerchantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MerchantPointTx_Merchant_Type_Created",
                table: "MerchantPointTransactions",
                columns: new[] { "MerchantId", "GrantType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_MerchantPointTransactions_BizId",
                table: "MerchantPointTransactions",
                column: "BizId",
                unique: true,
                filter: "\"BizId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MerchantPointAccounts");

            migrationBuilder.DropTable(
                name: "MerchantPointTransactions");

            migrationBuilder.DropIndex(
                name: "IX_MallItems_Owner_Audit",
                table: "MallItems");

            migrationBuilder.DropColumn(
                name: "ReceivedAt",
                table: "MallOrders");

            migrationBuilder.DropColumn(
                name: "ReceiverAddress",
                table: "MallOrders");

            migrationBuilder.DropColumn(
                name: "ReceiverName",
                table: "MallOrders");

            migrationBuilder.DropColumn(
                name: "ReceiverPhone",
                table: "MallOrders");

            migrationBuilder.DropColumn(
                name: "ShipStatus",
                table: "MallOrders");

            migrationBuilder.DropColumn(
                name: "ShipTracking",
                table: "MallOrders");

            migrationBuilder.DropColumn(
                name: "ShippedAt",
                table: "MallOrders");

            migrationBuilder.DropColumn(
                name: "AuditRemark",
                table: "MallItems");

            migrationBuilder.DropColumn(
                name: "AuditState",
                table: "MallItems");

            migrationBuilder.DropColumn(
                name: "OwnerMerchantId",
                table: "MallItems");

            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                table: "MallItems",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);
        }
    }
}
