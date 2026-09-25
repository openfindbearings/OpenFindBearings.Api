using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPointRewardClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PointRewardClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BizKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    GrantType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FirstClaimerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointRewardClaims", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_PointRewardClaims_BizKey",
                table: "PointRewardClaims",
                column: "BizKey",
                unique: true);
            // 改动说明（v1.34.0 防刷台账）：建表后一次性数据操作——
            //   1) 回填存量认领：已发过的注册奖励（join 用户手机号）与入驻奖励（join 商户信用代码）
            //      写入台账，防止上线后"注销重注册/删店重入驻"把历史已领的再领一遍
            //   2) 新增两条商户侧一次性规则：资料完善 50 / 首件上架 20（幂等 INSERT）
            //   3) register_bonus 默认停用：注册未接短信验证，零成本手机号可刷 50 分，
            //      接入短信后由运营在 Admin 积分任务页开启
            migrationBuilder.Sql(@"
                INSERT INTO ""PointRewardClaims"" (""Id"", ""BizKey"", ""GrantType"", ""FirstClaimerUserId"", ""CreatedAt"", ""IsActive"")
                SELECT gen_random_uuid(), 'phone:' || u.""Mobile"" || ':register', t.""GrantType"", t.""UserId"", now(), true
                FROM ""PointTransactions"" t
                JOIN ""Users"" u ON u.""Id"" = t.""UserId""
                WHERE t.""GrantType"" = 'register_bonus' AND u.""Mobile"" IS NOT NULL AND u.""Mobile"" <> ''
                ON CONFLICT (""BizKey"") DO NOTHING;

                INSERT INTO ""PointRewardClaims"" (""Id"", ""BizKey"", ""GrantType"", ""FirstClaimerUserId"", ""CreatedAt"", ""IsActive"")
                SELECT gen_random_uuid(), 'credit:' || m.""UnifiedSocialCreditCode"" || ':approved', t.""GrantType"", t.""UserId"", now(), true
                FROM ""PointTransactions"" t
                JOIN ""Merchants"" m
                  ON 'merchant_approved:' || m.""Id""::text || '' = t.""BizId""
                WHERE t.""GrantType"" = 'merchant_approved'
                  AND m.""UnifiedSocialCreditCode"" IS NOT NULL AND m.""UnifiedSocialCreditCode"" <> ''
                ON CONFLICT (""BizKey"") DO NOTHING;

                INSERT INTO ""PointGrantRules"" (""Id"", ""GrantType"", ""DisplayName"", ""Amount"", ""DailyLimit"", ""LadderJson"", ""IsEnabled"", ""Description"", ""CreatedAt"", ""IsActive"")
                SELECT gen_random_uuid(), v.g, v.n, v.a, 0, NULL, true, v.d, now(), true
                FROM (VALUES
                    ('merchant_profile_complete', '完善商户资料', 50, '联系人/电话/简介/地址首次齐全（同执照仅一次）'),
                    ('merchant_first_product', '首件商品上架', 20, '商户首个在售商品上架（同执照仅一次）')
                ) AS v(g, n, a, d)
                WHERE NOT EXISTS (SELECT 1 FROM ""PointGrantRules"" r WHERE r.""GrantType"" = v.g);

                UPDATE ""PointGrantRules"" SET ""IsEnabled"" = false, ""UpdatedAt"" = now()
                WHERE ""GrantType"" = 'register_bonus' AND ""IsEnabled"" = true;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PointRewardClaims");
        }
    }
}
