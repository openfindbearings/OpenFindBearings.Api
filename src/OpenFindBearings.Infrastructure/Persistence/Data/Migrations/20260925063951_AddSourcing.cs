using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSourcing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SourcingDemands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublisherUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BearingId = table.Column<Guid>(type: "uuid", nullable: true),
                    PartNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Brand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Quantity = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ExpectedDelivery = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Region = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExpiryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SelectedResponseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResponseCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourcingDemands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SourcingResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DemandId = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RespondedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    Stock = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LeadTime = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourcingResponses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SourcingDemands_PartNumber",
                table: "SourcingDemands",
                column: "PartNumber");

            migrationBuilder.CreateIndex(
                name: "IX_SourcingDemands_PublisherUserId",
                table: "SourcingDemands",
                column: "PublisherUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SourcingDemands_Status_CreatedAt",
                table: "SourcingDemands",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SourcingResponses_MerchantId",
                table: "SourcingResponses",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "UX_SourcingResponses_Demand_Merchant",
                table: "SourcingResponses",
                columns: new[] { "DemandId", "MerchantId" },
                unique: true);
            // 改动说明（v1.35.0 寻货种子）：两条积分加量规则（超限发布/应答按次扣分，
            //   DailyLimit 为硬上限防无限积分刷）+ 两条免费额度配置（Admin 系统配置页可改）
            migrationBuilder.Sql(@"
                INSERT INTO ""PointGrantRules"" (""Id"", ""GrantType"", ""DisplayName"", ""Amount"", ""DailyLimit"", ""LadderJson"", ""IsEnabled"", ""Description"", ""CreatedAt"", ""IsActive"")
                SELECT gen_random_uuid(), v.g, v.n, v.a, v.l, NULL, true, v.d, now(), true
                FROM (VALUES
                    ('sourcing_publish_bonus', '寻货发布积分加量', 20, 10, '免费额度用完后花积分继续发布（20 分/条，硬上限 10 条/天）'),
                    ('sourcing_respond_bonus', '寻货应答积分加量', 20, 50, '免费额度用完后花积分继续应答（20 分/条，硬上限 50 条/天）')
                ) AS v(g, n, a, l, d)
                WHERE NOT EXISTS (SELECT 1 FROM ""PointGrantRules"" r WHERE r.""GrantType"" = v.g);

                INSERT INTO ""SystemConfigs"" (""Id"", ""Key"", ""Value"", ""Description"", ""Group"", ""ValueType"", ""IsSystem"", ""CreatedAt"", ""UpdatedAt"", ""IsActive"")
                SELECT gen_random_uuid(), v.k, v.val, v.d, 'Sourcing', 'number', false, now(), now(), true
                FROM (VALUES
                    ('Sourcing.FreePublishPerDay', '3', '个人每日免费发布寻货条数（超出后每条扣积分）'),
                    ('Sourcing.FreeRespondPerDay', '20', '商户每日免费应答条数（超出后每条扣积分）')
                ) AS v(k, val, d)
                WHERE NOT EXISTS (SELECT 1 FROM ""SystemConfigs"" s WHERE s.""Key"" = v.k);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SourcingDemands");

            migrationBuilder.DropTable(
                name: "SourcingResponses");
        }
    }
}
