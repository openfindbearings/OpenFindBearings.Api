using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSourcingTargetMerchant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TargetMerchantId",
                table: "SourcingDemands",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SourcingDemands_TargetMerchantId",
                table: "SourcingDemands",
                column: "TargetMerchantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SourcingDemands_TargetMerchantId",
                table: "SourcingDemands");

            migrationBuilder.DropColumn(
                name: "TargetMerchantId",
                table: "SourcingDemands");
        }
    }
}
