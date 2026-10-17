using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMerchantOnboardedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OnboardedAt",
                table: "Merchants",
                type: "timestamp with time zone",
                nullable: true);

            // 改动说明（v1.50.0 入驻统计回填）：存量已生效（Status 0=Active / 1=Suspended）且经入驻流程
            //   （ApplicationMode 1=Self / 2=Claim / 3=Nomination）的商家，以 CreatedAt 作为入驻生效时间；
            //   互联网/种子/历史（ApplicationMode 0=None）与未生效（Status 2=Pending / 3=Draft）留空。
            //   幂等：仅回填 OnboardedAt 为空的行。
            migrationBuilder.Sql(
                "UPDATE \"Merchants\" SET \"OnboardedAt\" = \"CreatedAt\" " +
                "WHERE \"ApplicationMode\" <> 0 AND \"Status\" IN (0, 1) AND \"OnboardedAt\" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnboardedAt",
                table: "Merchants");
        }
    }
}
