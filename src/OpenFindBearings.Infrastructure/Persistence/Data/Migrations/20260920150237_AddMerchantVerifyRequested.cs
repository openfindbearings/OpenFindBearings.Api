using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// v2.9.0 申请认证闭环：Merchants 加 VerifyRequested 布尔列（默认 false，存量无需回填）。
    /// 商户主动申请认证置 true，Admin 认证通过时清除，仅作审核队列优先级信号。
    /// </summary>
    public partial class AddMerchantVerifyRequested : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "VerifyRequested",
                table: "Merchants",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VerifyRequested",
                table: "Merchants");
        }
    }
}
