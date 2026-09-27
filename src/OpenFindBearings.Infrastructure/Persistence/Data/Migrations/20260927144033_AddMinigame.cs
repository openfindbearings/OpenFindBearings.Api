using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMinigame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 游戏中心（v2.10.1）：连连看胜利发分规则——每局 5 分、日上限 10 分（约 2 局），
            // 幂等避让旧行；Admin 积分规则页可调额与限
            migrationBuilder.Sql(@"
INSERT INTO ""PointGrantRules"" (""Id"", ""GrantType"", ""DisplayName"", ""Amount"", ""DailyLimit"", ""LadderJson"", ""IsEnabled"", ""Description"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), 'minigame', '小游戏胜利奖励', 5, 10, NULL, true, '轴承连连看等平台小游戏胜利结算发放（每局按 gameId 幂等，日限防刷）', now(), true
WHERE NOT EXISTS (SELECT 1 FROM ""PointGrantRules"" WHERE ""GrantType"" = 'minigame');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM ""PointGrantRules"" WHERE ""GrantType"" = 'minigame';");
        }
    }
}
