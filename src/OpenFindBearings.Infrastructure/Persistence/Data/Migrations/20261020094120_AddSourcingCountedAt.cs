using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSourcingCountedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CountedAt",
                table: "SourcingResponses",
                type: "timestamp with time zone",
                nullable: true);

            // 改动说明（v2.19.0 审计修复）：存量已计分回执以原回执时间为首次计分时间，
            // 避免升级后这些成交因 CountedAt 为空而集体从有效期中消失
            migrationBuilder.Sql(@"
                UPDATE ""SourcingResponses""
                SET ""CountedAt"" = ""FeedbackAt""
                WHERE ""IsCounted"" = true AND ""CountedAt"" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountedAt",
                table: "SourcingResponses");
        }
    }
}
