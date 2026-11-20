using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchPinyinKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SearchInitials",
                table: "Merchants",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SearchPinyin",
                table: "Merchants",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SearchPinyinFuzzy",
                table: "Merchants",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SearchInitials",
                table: "Brands",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SearchPinyin",
                table: "Brands",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SearchPinyinFuzzy",
                table: "Brands",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SearchInitials",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "SearchPinyin",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "SearchPinyinFuzzy",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "SearchInitials",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "SearchPinyin",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "SearchPinyinFuzzy",
                table: "Brands");
        }
    }
}
