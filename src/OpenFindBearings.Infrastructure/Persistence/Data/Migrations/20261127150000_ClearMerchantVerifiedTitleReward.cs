using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <summary>
    /// 清理商户认证成就的重复称号（v1.59.25）：认证状态由 Merchant.IsVerified 表达（分享标题、
    /// 商家标识、Admin 认证页签都基于它），成就系统不应再给一条「认证商家」称号——称号是个人侧概念
    /// （User.EquippedTitle），商家维度没有佩戴入口。此迁移把该成就的 TitleReward 置空。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261127150000_ClearMerchantVerifiedTitleReward")]
    public partial class ClearMerchantVerifiedTitleReward : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "AchievementDefinitions"
                SET "TitleReward" = NULL, "UpdatedAt" = now()
                WHERE "Key" = 'merchant_verified';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "AchievementDefinitions"
                SET "TitleReward" = '认证商家', "UpdatedAt" = now()
                WHERE "Key" = 'merchant_verified';
                """);
        }
    }
}