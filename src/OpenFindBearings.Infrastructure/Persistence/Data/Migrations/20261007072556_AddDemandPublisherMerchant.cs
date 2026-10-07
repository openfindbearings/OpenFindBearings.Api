using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDemandPublisherMerchant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PublisherMerchantId",
                table: "SourcingDemands",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublisherMerchantName",
                table: "SourcingDemands",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SourcingDemands_PublisherMerchantId",
                table: "SourcingDemands",
                column: "PublisherMerchantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SourcingDemands_PublisherMerchantId",
                table: "SourcingDemands");

            migrationBuilder.DropColumn(
                name: "PublisherMerchantId",
                table: "SourcingDemands");

            migrationBuilder.DropColumn(
                name: "PublisherMerchantName",
                table: "SourcingDemands");
        }
    }
}
