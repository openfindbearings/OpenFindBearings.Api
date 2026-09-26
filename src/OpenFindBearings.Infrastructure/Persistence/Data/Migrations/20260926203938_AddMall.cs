using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMall : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PinnedUntil",
                table: "MerchantBearings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MallItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Icon = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    PointPrice = table.Column<int>(type: "integer", nullable: false),
                    FlashPrice = table.Column<int>(type: "integer", nullable: true),
                    FlashStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FlashEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationHours = table.Column<int>(type: "integer", nullable: true),
                    Stock = table.Column<int>(type: "integer", nullable: false),
                    SoldCount = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MallOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ItemName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PointsSpent = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TargetRef = table.Column<Guid>(type: "uuid", nullable: true),
                    Remark = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    FulfilledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallOrders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_MallItems_Key",
                table: "MallItems",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MallOrders_User_Created",
                table: "MallOrders",
                columns: new[] { "UserId", "CreatedAt" });

            // 改动说明（v2.3.0 商城虚拟权益）：初始置顶卡目录走迁移而非 SeedData——
            // 存量库 SeedData 按 Users 非空整体短路永不执行，迁移是唯一可靠落点；幂等 NOT EXISTS。
            // 定价锚（承设计 v2.1.0）：月入约 180 分 → 24h 置顶 120、72h 置顶 320；
            // Category=1 置顶卡；Stock=-1 不限量（稀缺感后续由限量闪购制造）
            migrationBuilder.Sql(@"
INSERT INTO ""MallItems"" (""Id"", ""Key"", ""Name"", ""Description"", ""Icon"", ""Category"", ""PointPrice"", ""FlashPrice"", ""FlashStart"", ""FlashEnd"", ""DurationHours"", ""Stock"", ""SoldCount"", ""Enabled"", ""SortOrder"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), v.""Key"", v.""Name"", v.""Description"", v.""Icon"", v.""Category"", v.""Price"", NULL, NULL, NULL, v.""Hours"", v.""Stock"", 0, true, v.""Sort"", now(), true
FROM (VALUES
 ('pin_24h', '24小时置顶卡', '把一件在售商品在该型号商家列表置顶 24 小时，买家一眼先看到你', 'arrow-up-circle', 1, 120, 24, -1, 10),
 ('pin_72h', '72小时置顶卡', '把一件在售商品在该型号商家列表置顶 72 小时，长曝光更划算', 'rocket', 1, 320, 72, -1, 20)
) AS v(""Key"", ""Name"", ""Description"", ""Icon"", ""Category"", ""Price"", ""Hours"", ""Stock"", ""Sort"")
WHERE NOT EXISTS (SELECT 1 FROM ""MallItems"" m WHERE m.""Key"" = v.""Key"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MallItems");

            migrationBuilder.DropTable(
                name: "MallOrders");

            migrationBuilder.DropColumn(
                name: "PinnedUntil",
                table: "MerchantBearings");
        }
    }
}
