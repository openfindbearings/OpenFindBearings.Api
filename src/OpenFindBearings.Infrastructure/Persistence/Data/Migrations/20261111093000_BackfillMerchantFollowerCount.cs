using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <summary>
    /// 回填商家粉丝计数（纯数据迁移，不改表结构）。
    /// 背景：关注命令历史上只写 UserMerchantFollows 连接表，从未更新 Merchants.FollowerCount，
    /// 导致该列在所有商家上恒为 0；本迁移以关注表为事实来源重算一次，并清零无关注记录的残留值。
    /// 手写迁移无 Designer，显式 MigrationAttribute 固定 Id 保证排序在 AddBrandProfileAndBearingTypeDetails 之后。
    /// </summary>
    [Migration("20261111093000_BackfillMerchantFollowerCount")]
    public partial class BackfillMerchantFollowerCount : Migration
    {
        /// <summary>
        /// 按连接表实际关注数回填 Merchants.FollowerCount
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE ""Merchants"" AS m
SET ""FollowerCount"" = c.cnt
FROM (
    SELECT ""MerchantId"", COUNT(*) AS cnt
    FROM ""UserMerchantFollows""
    GROUP BY ""MerchantId""
) AS c
WHERE m.""Id"" = c.""MerchantId"";
");

            migrationBuilder.Sql(@"
UPDATE ""Merchants"" AS m
SET ""FollowerCount"" = 0
WHERE m.""FollowerCount"" <> 0
  AND NOT EXISTS (
      SELECT 1 FROM ""UserMerchantFollows"" AS f WHERE f.""MerchantId"" = m.""Id""
  );
");
        }

        /// <summary>
        /// 数据回填无需回滚：计数为派生值，回滚保持现状即可
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 派生数据不设回滚目标：关注记录本身未改动
        }
    }
}