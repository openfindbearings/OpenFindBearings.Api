using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMerchantBearingRestocking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRestocking",
                table: "MerchantBearings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RestockEta",
                table: "MerchantBearings",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRestocking",
                table: "MerchantBearings");

            migrationBuilder.DropColumn(
                name: "RestockEta",
                table: "MerchantBearings");
        }
    }
}
