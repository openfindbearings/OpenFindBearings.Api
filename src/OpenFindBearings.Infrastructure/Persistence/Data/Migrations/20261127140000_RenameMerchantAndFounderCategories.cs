using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <summary>
    /// 成就分类改名第二批（v1.59.24）：把「元老」改为「创站元老」、「商户」改为「经营有方」。
    /// 与上一批（忠诚→细水长流、寻货→一拍即合）同性质：只改展示分类名，
    /// 不动勋章 Key、指标、进度与解锁逻辑；前端页签按接口 category 聚合，无需改动。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261127140000_RenameMerchantAndFounderCategories")]
    public partial class RenameMerchantAndFounderCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 资历类：与勋章名「创站元老」对齐，突出稀缺性
            migrationBuilder.Sql("""
                UPDATE "AchievementDefinitions"
                SET "Category" = '创站元老', "UpdatedAt" = now()
                WHERE "Category" = '元老';
                """);

            // 商家维度：5 枚商家成就，用"经营有方"表达做事有章法，避免与"商户"实体名混淆
            migrationBuilder.Sql("""
                UPDATE "AchievementDefinitions"
                SET "Category" = '经营有方', "UpdatedAt" = now()
                WHERE "Category" = '商户';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "AchievementDefinitions"
                SET "Category" = '元老', "UpdatedAt" = now()
                WHERE "Category" = '创站元老';
                """);

            migrationBuilder.Sql("""
                UPDATE "AchievementDefinitions"
                SET "Category" = '商户', "UpdatedAt" = now()
                WHERE "Category" = '经营有方';
                """);
        }
    }
}