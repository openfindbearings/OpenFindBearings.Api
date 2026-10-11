using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBearingSaleReviewState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) 删旧索引
            migrationBuilder.DropIndex(
                name: "IX_MerchantBearings_IsOnSale",
                table: "MerchantBearings");

            migrationBuilder.DropIndex(
                name: "IX_MerchantBearings_IsPendingApproval",
                table: "MerchantBearings");

            // 2) 先加新列（临时默认 0，随后回填）
            migrationBuilder.AddColumn<int>(
                name: "ReviewStatus",
                table: "MerchantBearings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SaleState",
                table: "MerchantBearings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // 3) 从旧列回填（v2.15.0 状态统一，保证存量/爬虫数据保持公域可见）：
            //    SaleState: IsOnSale→1(出售中) / IsRestocking→2(补货中) / 否则 0(已下架)
            //    ReviewStatus: IsPendingApproval→1(审核中) / 否则 2(已通过)
            migrationBuilder.Sql(
                "UPDATE \"MerchantBearings\" SET " +
                "\"SaleState\" = CASE WHEN \"IsOnSale\" THEN 1 WHEN \"IsRestocking\" THEN 2 ELSE 0 END, " +
                "\"ReviewStatus\" = CASE WHEN \"IsPendingApproval\" THEN 1 ELSE 2 END;");

            // 4) 删旧列（含僵尸 IsFeatured）
            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "MerchantBearings");

            migrationBuilder.DropColumn(
                name: "IsOnSale",
                table: "MerchantBearings");

            migrationBuilder.DropColumn(
                name: "IsPendingApproval",
                table: "MerchantBearings");

            migrationBuilder.DropColumn(
                name: "IsRestocking",
                table: "MerchantBearings");

            // 5) 建新索引
            migrationBuilder.CreateIndex(
                name: "IX_MerchantBearings_ReviewStatus",
                table: "MerchantBearings",
                column: "ReviewStatus");

            migrationBuilder.CreateIndex(
                name: "IX_MerchantBearings_SaleState",
                table: "MerchantBearings",
                column: "SaleState");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MerchantBearings_ReviewStatus",
                table: "MerchantBearings");

            migrationBuilder.DropIndex(
                name: "IX_MerchantBearings_SaleState",
                table: "MerchantBearings");

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "MerchantBearings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnSale",
                table: "MerchantBearings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPendingApproval",
                table: "MerchantBearings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRestocking",
                table: "MerchantBearings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // 从新列回填旧列（可逆）
            migrationBuilder.Sql(
                "UPDATE \"MerchantBearings\" SET " +
                "\"IsOnSale\" = (\"SaleState\" = 1), " +
                "\"IsRestocking\" = (\"SaleState\" = 2), " +
                "\"IsPendingApproval\" = (\"ReviewStatus\" = 1);");

            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "MerchantBearings");

            migrationBuilder.DropColumn(
                name: "SaleState",
                table: "MerchantBearings");

            migrationBuilder.CreateIndex(
                name: "IX_MerchantBearings_IsOnSale",
                table: "MerchantBearings",
                column: "IsOnSale");

            migrationBuilder.CreateIndex(
                name: "IX_MerchantBearings_IsPendingApproval",
                table: "MerchantBearings",
                column: "IsPendingApproval");
        }
    }
}
