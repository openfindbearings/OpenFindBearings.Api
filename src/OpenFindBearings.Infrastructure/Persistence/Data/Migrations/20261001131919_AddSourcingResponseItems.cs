using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSourcingResponseItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // v1.5.0 多行标书：先建行表，把旧单型号应答（价格/库存/交期/需求型号）回填为一行，
            // 再删主表内容列——顺序不可反，否则旧应答数据丢失
            migrationBuilder.CreateTable(
                name: "SourcingResponseItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BearingId = table.Column<Guid>(type: "uuid", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    Stock = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LeadTime = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourcingResponseItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SourcingResponseItems_SourcingResponses_ResponseId",
                        column: x => x.ResponseId,
                        principalTable: "SourcingResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SourcingResponseItems_ResponseId",
                table: "SourcingResponseItems",
                column: "ResponseId");

            // 回填：每条旧应答按需求型号生成一行（引用在售为空——旧应答未关联在售商品）
            migrationBuilder.Sql(
                """
                INSERT INTO "SourcingResponseItems"
                    ("Id", "ResponseId", "PartNumber", "BearingId", "Price", "Stock", "LeadTime", "CreatedAt", "UpdatedAt", "IsActive")
                SELECT gen_random_uuid(),
                       r."Id",
                       d."PartNumber",
                       NULL,
                       r."Price",
                       r."Stock",
                       r."LeadTime",
                       r."CreatedAt",
                       r."UpdatedAt",
                       TRUE
                FROM "SourcingResponses" r
                JOIN "SourcingDemands" d ON d."Id" = r."DemandId";
                """);

            migrationBuilder.DropColumn(
                name: "LeadTime",
                table: "SourcingResponses");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "SourcingResponses");

            migrationBuilder.DropColumn(
                name: "Stock",
                table: "SourcingResponses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SourcingResponseItems");

            migrationBuilder.AddColumn<string>(
                name: "LeadTime",
                table: "SourcingResponses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "SourcingResponses",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Stock",
                table: "SourcingResponses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
