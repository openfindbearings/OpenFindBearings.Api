using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBearingPartNumberLeads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BearingLeads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PartNumberKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    OccurrenceCount = table.Column<int>(type: "integer", nullable: false),
                    FirstSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsIgnored = table.Column<bool>(type: "boolean", nullable: false),
                    IgnoredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IgnoredByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BearingLeads", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BearingLeads_LastSeenAt",
                table: "BearingLeads",
                column: "LastSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_BearingLeads_OccurrenceCount",
                table: "BearingLeads",
                column: "OccurrenceCount");

            migrationBuilder.CreateIndex(
                name: "UX_BearingLeads_PartNumberKey",
                table: "BearingLeads",
                column: "PartNumberKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BearingLeads");
        }
    }
}
