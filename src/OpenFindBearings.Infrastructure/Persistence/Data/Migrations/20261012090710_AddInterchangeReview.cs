using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInterchangeReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProposedByMerchantId",
                table: "BearingInterchanges",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewComment",
                table: "BearingInterchanges",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewStatus",
                table: "BearingInterchanges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_BearingInterchanges_ReviewStatus",
                table: "BearingInterchanges",
                column: "ReviewStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BearingInterchanges_ReviewStatus",
                table: "BearingInterchanges");

            migrationBuilder.DropColumn(
                name: "ProposedByMerchantId",
                table: "BearingInterchanges");

            migrationBuilder.DropColumn(
                name: "ReviewComment",
                table: "BearingInterchanges");

            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "BearingInterchanges");
        }
    }
}
