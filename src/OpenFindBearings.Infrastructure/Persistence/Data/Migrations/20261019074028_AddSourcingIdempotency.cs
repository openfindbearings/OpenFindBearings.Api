using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <summary>
    /// 寻货发布幂等键迁移（v1.8.2 彻底幂等）：给 SourcingDemands 加可空 IdempotencyKey，
    /// 并建"同发布人同 key"的过滤唯一索引（IdempotencyKey IS NOT NULL 才参与约束，
    /// 未提供幂等键的运行不受影响）。加列与建索引均对存量数据无破坏（Null 行绕开唯一约束）。
    /// </summary>
    public partial class AddSourcingIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "SourcingDemands",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_SourcingDemands_Publisher_IdemKey",
                table: "SourcingDemands",
                columns: new[] { "PublisherUserId", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_SourcingDemands_Publisher_IdemKey",
                table: "SourcingDemands");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "SourcingDemands");
        }
    }
}
