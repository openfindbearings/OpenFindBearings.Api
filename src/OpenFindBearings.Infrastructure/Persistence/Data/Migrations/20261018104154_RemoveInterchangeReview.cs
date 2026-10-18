using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <summary>
    /// 替代品字典化（v2.17.0）：撤销替代关系的"商家候选 + 平台审核"状态机——
    /// 删除 ReviewStatus/ReviewComment/ProposedByMerchantId 三列与对应索引；
    /// 生效规则回归"进 API 即生效"（爬取候选在 Sync staging 审核，平台在管理页人工维护）。
    /// 迁移前物理清除从未生效的历史候选行（待审核/已驳回），避免遗留无出口数据。
    /// </summary>
    public partial class RemoveInterchangeReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 改动说明（v2.17.0 替代品字典化）：删除"从未生效"行（ReviewStatus：1=待审核 2=已驳回），
            //   仅保留 0=已生效；字典化后这些行既不能生效也不应继续留存
            migrationBuilder.Sql("DELETE FROM \"BearingInterchanges\" WHERE \"ReviewStatus\" <> 0;");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
    }
}
