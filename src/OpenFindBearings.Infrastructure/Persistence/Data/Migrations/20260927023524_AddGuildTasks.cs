using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGuildTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildTaskCompletions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MerchantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodKey = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    MetricValue = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildTaskCompletions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildTaskDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MetricKey = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetValue = table.Column<int>(type: "integer", nullable: false),
                    Period = table.Column<int>(type: "integer", nullable: false),
                    RewardType = table.Column<int>(type: "integer", nullable: false),
                    RewardAmount = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildTaskDefinitions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_GuildTaskCompletions_Task_Merchant_Period",
                table: "GuildTaskCompletions",
                columns: new[] { "TaskKey", "MerchantId", "PeriodKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_GuildTaskDefinitions_Key",
                table: "GuildTaskDefinitions",
                column: "TaskKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildTaskCompletions");

            migrationBuilder.DropTable(
                name: "GuildTaskDefinitions");
        }
    }
}
