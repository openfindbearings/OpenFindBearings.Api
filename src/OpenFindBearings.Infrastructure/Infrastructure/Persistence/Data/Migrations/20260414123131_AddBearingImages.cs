using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBearingImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Image2DCAD",
                table: "Bearings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Image3D",
                table: "Bearings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LimitingSpeedGrease",
                table: "Bearings",
                type: "numeric(10,0)",
                precision: 10,
                scale: 0,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LimitingSpeedOil",
                table: "Bearings",
                type: "numeric(10,0)",
                precision: 10,
                scale: 0,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Image2DCAD",
                table: "Bearings");

            migrationBuilder.DropColumn(
                name: "Image3D",
                table: "Bearings");

            migrationBuilder.DropColumn(
                name: "LimitingSpeedGrease",
                table: "Bearings");

            migrationBuilder.DropColumn(
                name: "LimitingSpeedOil",
                table: "Bearings");
        }
    }
}
