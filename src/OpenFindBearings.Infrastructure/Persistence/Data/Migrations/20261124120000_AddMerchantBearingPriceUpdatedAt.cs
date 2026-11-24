using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <summary>
    /// 商家商品增加「报价最后变更时间」列（v1.59.22）：仅价格描述实际变化时刷新，
    /// 供型号详情展示「报价更新于」，与 UpdatedAt（任意字段变更）区分。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261124120000_AddMerchantBearingPriceUpdatedAt")]
    public partial class AddMerchantBearingPriceUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PriceUpdatedAt",
                table: "MerchantBearings",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceUpdatedAt",
                table: "MerchantBearings");
        }
    }
}