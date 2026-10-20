using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMerchantGradePeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "GradePeriodEndAt",
                table: "Merchants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GradePeriodStartAt",
                table: "Merchants",
                type: "timestamp with time zone",
                nullable: true);

            // 改动说明（v2.19.0 有效期制）：存量已认证商户回填一个完整有效期，避免上线首日
            // 全部无到期日、且巡检到点即按"本期 0 成交"集体降档；未认证商户保持为空（普通无有效期）
            migrationBuilder.Sql(@"
                UPDATE ""Merchants""
                SET ""GradePeriodStartAt"" = now(),
                    ""GradePeriodEndAt"" = now() + interval '180 days'
                WHERE ""IsVerified"" = true;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GradePeriodEndAt",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "GradePeriodStartAt",
                table: "Merchants");
        }
    }
}
