using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenFindBearings.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// 成就进度历史回填 + 僵尸列清理（v2.13.1 提交缺失修复）。
    /// 背景：成就写路径自 v2.1.0 起几乎从未落库——签到端点/注册与登录中间件在积分提交之后
    /// 才调成就、MediatR handler 在 UnitOfWorkBehavior 提交之后才被 Publish，变更随 DbContext
    /// 释放丢失（真机现象：累计签到 30+ 仍显示 0/30）。服务侧提交修复见 AchievementService。
    /// 本迁移从流水与业务事实表一次性回填历史进度，幂等可重复执行：
    ///   checkin_total/login_total/correction_adopted = PointTransactions 按 GrantType 计数
    ///   checkin_streak = PointAccounts.ConsecutiveCheckinDays
    ///   sourcing_selected/merchant_selected = SourcingResponses.Status=2（已采纳）
    ///   sourcing_publish_total = SourcingDemands 按发布人计数；register_total = Users 每人 1
    ///   listing_count = MerchantBearings.IsOnSale 按商户计数
    /// 冲突策略：ON CONFLICT 保 GREATEST（进度只增不减）、COALESCE（已点亮不熄），重复执行不劣化。
    /// 同时清理僵尸列 AchievementDefinitions.RewardPoints——实体已于 v2.12.0 删除该字段，
    /// 但当时未出迁移，DB 中残留无任何读写路径引用的死列。
    /// 手写迁移无 Designer，显式 MigrationAttribute 固定 Id 保证排序在 RenamePremiumMerchantGradeLabel 之后
    /// </summary>
    [Migration("20261010130000_BackfillAchievementProgress")]
    public partial class BackfillAchievementProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 僵尸列清理：RewardPoints 已从实体删除，DB 残留死列一并移除
            migrationBuilder.DropColumn(name: "RewardPoints", table: "AchievementDefinitions");

            // 历史进度回填：按 metricKey 聚合事实源，JOIN 定义表算出应点亮的成就行
            migrationBuilder.Sql(@"
WITH progress AS (
    SELECT 1 AS scope, t.""UserId"" AS owner, 'checkin_total' AS metric, COUNT(*)::int AS p
    FROM ""PointTransactions"" t WHERE t.""GrantType"" = 'daily_checkin'
    GROUP BY t.""UserId""
    UNION ALL
    SELECT 1, a.""UserId"", 'checkin_streak', a.""ConsecutiveCheckinDays""
    FROM ""PointAccounts"" a WHERE a.""ConsecutiveCheckinDays"" > 0
    UNION ALL
    SELECT 1, t.""UserId"", 'login_total', COUNT(*)::int
    FROM ""PointTransactions"" t WHERE t.""GrantType"" = 'daily_login'
    GROUP BY t.""UserId""
    UNION ALL
    SELECT 1, t.""UserId"", 'correction_adopted', COUNT(*)::int
    FROM ""PointTransactions"" t WHERE t.""GrantType"" = 'correction_adopted'
    GROUP BY t.""UserId""
    UNION ALL
    SELECT 1, r.""RespondedUserId"", 'sourcing_selected', COUNT(*)::int
    FROM ""SourcingResponses"" r WHERE r.""Status"" = 2
    GROUP BY r.""RespondedUserId""
    UNION ALL
    SELECT 1, d.""PublisherUserId"", 'sourcing_publish_total', COUNT(*)::int
    FROM ""SourcingDemands"" d
    GROUP BY d.""PublisherUserId""
    UNION ALL
    SELECT 1, u.""Id"", 'register_total', 1
    FROM ""Users"" u
    UNION ALL
    SELECT 2, m.""MerchantId"", 'listing_count', COUNT(*)::int
    FROM ""MerchantBearings"" m WHERE m.""IsOnSale""
    GROUP BY m.""MerchantId""
    UNION ALL
    SELECT 2, r.""MerchantId"", 'merchant_selected', COUNT(*)::int
    FROM ""SourcingResponses"" r WHERE r.""Status"" = 2
    GROUP BY r.""MerchantId""
)
INSERT INTO ""AchievementUnlocks"" (""Id"", ""Scope"", ""OwnerId"", ""AchievementKey"", ""Progress"", ""UnlockedAt"", ""CreatedAt"", ""IsActive"")
SELECT gen_random_uuid(), pp.scope, pp.owner, d.""Key"", pp.p,
    CASE WHEN pp.p >= d.""ProgressTarget"" THEN now() ELSE NULL END,
    now(), true
FROM progress pp
JOIN ""AchievementDefinitions"" d
    ON d.""MetricKey"" = pp.metric AND d.""Scope"" = pp.scope AND d.""Enabled"" AND d.""IsActive""
ON CONFLICT (""Scope"", ""OwnerId"", ""AchievementKey"") DO UPDATE
SET ""Progress"" = GREATEST(""AchievementUnlocks"".""Progress"", EXCLUDED.""Progress""),
    ""UnlockedAt"" = COALESCE(""AchievementUnlocks"".""UnlockedAt"", EXCLUDED.""UnlockedAt""),
    ""UpdatedAt"" = now();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 数据回填无法干净撤销（无法区分回填行与真实解锁行），仅结构性还原僵尸列以保持可迁移性
            migrationBuilder.AddColumn<int>(
                name: "RewardPoints",
                table: "AchievementDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}