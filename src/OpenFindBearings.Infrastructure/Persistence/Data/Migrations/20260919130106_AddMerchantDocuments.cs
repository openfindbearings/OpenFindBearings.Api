using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// v2.7.0 证照材料泛化：LicenseVerifications 表整体重命名为 MerchantDocuments
    /// （LicenseUrl 列改名 FileUrl、新增 Type 列并回填存量为 1 营业执照、索引同步改名），
    /// 手写为 Rename 系列以保留存量审核记录（EF 自动脚手架的 Drop/Create 会丢数据，已替换）。
    /// </summary>
    public partial class AddMerchantDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 表与列重命名（数据无损）
            migrationBuilder.RenameTable(
                name: "LicenseVerifications",
                newName: "MerchantDocuments");

            migrationBuilder.RenameColumn(
                name: "LicenseUrl",
                table: "MerchantDocuments",
                newName: "FileUrl");

            // 索引改名（沿用 RenameTable 后仍带旧前缀的物理索引名，对齐新模型快照）
            migrationBuilder.RenameIndex(
                name: "IX_LicenseVerifications_MerchantId",
                table: "MerchantDocuments",
                newName: "IX_MerchantDocuments_MerchantId");

            migrationBuilder.RenameIndex(
                name: "IX_LicenseVerifications_ReviewedBy",
                table: "MerchantDocuments",
                newName: "IX_MerchantDocuments_ReviewedBy");

            migrationBuilder.RenameIndex(
                name: "IX_LicenseVerifications_Status",
                table: "MerchantDocuments",
                newName: "IX_MerchantDocuments_Status");

            migrationBuilder.RenameIndex(
                name: "IX_LicenseVerifications_SubmittedAt",
                table: "MerchantDocuments",
                newName: "IX_MerchantDocuments_SubmittedAt");

            migrationBuilder.RenameIndex(
                name: "IX_LicenseVerifications_SubmittedBy",
                table: "MerchantDocuments",
                newName: "IX_MerchantDocuments_SubmittedBy");

            // 新列：材料类型，存量行一次性回填为 1（营业执照）——旧表本就只承载执照
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "MerchantDocuments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // 认证口径查询索引（商户+材料类型）
            migrationBuilder.CreateIndex(
                name: "IX_MerchantDocuments_MerchantId_Type",
                table: "MerchantDocuments",
                columns: new[] { "MerchantId", "Type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MerchantDocuments_MerchantId_Type",
                table: "MerchantDocuments");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "MerchantDocuments");

            migrationBuilder.RenameColumn(
                name: "FileUrl",
                table: "MerchantDocuments",
                newName: "LicenseUrl");

            migrationBuilder.RenameIndex(
                name: "IX_MerchantDocuments_MerchantId",
                table: "MerchantDocuments",
                newName: "IX_LicenseVerifications_MerchantId");

            migrationBuilder.RenameIndex(
                name: "IX_MerchantDocuments_ReviewedBy",
                table: "MerchantDocuments",
                newName: "IX_LicenseVerifications_ReviewedBy");

            migrationBuilder.RenameIndex(
                name: "IX_MerchantDocuments_Status",
                table: "MerchantDocuments",
                newName: "IX_LicenseVerifications_Status");

            migrationBuilder.RenameIndex(
                name: "IX_MerchantDocuments_SubmittedAt",
                table: "MerchantDocuments",
                newName: "IX_LicenseVerifications_SubmittedAt");

            migrationBuilder.RenameIndex(
                name: "IX_MerchantDocuments_SubmittedBy",
                table: "MerchantDocuments",
                newName: "IX_LicenseVerifications_SubmittedBy");

            migrationBuilder.RenameTable(
                name: "MerchantDocuments",
                newName: "LicenseVerifications");
        }
    }
}
