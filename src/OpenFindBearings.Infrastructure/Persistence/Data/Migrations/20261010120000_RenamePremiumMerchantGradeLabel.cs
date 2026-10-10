using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// 商家 Lv3 改名"活跃供给"→"口碑商家"（v2.13.0 等级玩法：四档统一"XX商家"格式）。
    /// 展示名硬编码在 Merchant.GetGradeDisplayName（代码改），本迁移只同步配置库描述文案
    /// （SystemConfigs.Description 展示在 Admin 系统配置页，避免旧名误导运营）；纯数据无 schema 变更。
    /// 手写迁移无 Designer，显式 MigrationAttribute 固定 Id 保证排序在 AddLevelGameplay 之后
    /// </summary>
    [Migration("20261010120000_RenamePremiumMerchantGradeLabel")]
    public partial class RenamePremiumMerchantGradeLabel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE ""SystemConfigs"" SET ""Description"" = replace(""Description"", '活跃供给', '口碑商家')
WHERE ""Description"" LIKE '%活跃供给%';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE ""SystemConfigs"" SET ""Description"" = replace(""Description"", '口碑商家', '活跃供给')
WHERE ""Description"" LIKE '%口碑商家%';");
        }
    }
}
