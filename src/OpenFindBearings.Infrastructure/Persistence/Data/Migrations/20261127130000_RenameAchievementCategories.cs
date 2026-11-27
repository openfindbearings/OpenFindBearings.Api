using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <summary>
    /// 成就分类改名（v1.59.23）：把「忠诚」改为「细水长流」、「寻货」改为「一拍即合」。
    /// 只改展示用的分类名，不涉及勋章 Key、指标与进度；页面页签按接口返回的 category 聚合，前端无需改动。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261127130000_RenameAchievementCategories")]
    public partial class RenameAchievementCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 签到类：连续/累计签到，本质是使用习惯，不再使用"忠诚"这种要求用户表态的措辞
            migrationBuilder.Sql("""
                UPDATE "AchievementDefinitions"
                SET "Category" = '细水长流', "UpdatedAt" = now()
                WHERE "Category" = '忠诚';
                """);

            // 寻货类：勋章口径是"应答被选定"，讲的是供需撮合，不再直白使用业务名"寻货"
            migrationBuilder.Sql("""
                UPDATE "AchievementDefinitions"
                SET "Category" = '一拍即合', "UpdatedAt" = now()
                WHERE "Category" = '寻货';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "AchievementDefinitions"
                SET "Category" = '忠诚', "UpdatedAt" = now()
                WHERE "Category" = '细水长流';
                """);

            migrationBuilder.Sql("""
                UPDATE "AchievementDefinitions"
                SET "Category" = '寻货', "UpdatedAt" = now()
                WHERE "Category" = '一拍即合';
                """);
        }
    }
}