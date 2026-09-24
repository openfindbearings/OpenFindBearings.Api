using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 积分三表配置（账户/流水/规则）：v1.32.0 积分底座
    /// </summary>
    public class PointConfiguration :
        IEntityTypeConfiguration<PointAccount>,
        IEntityTypeConfiguration<PointTransaction>,
        IEntityTypeConfiguration<PointGrantRule>
    {
        /// <summary>
        /// 配置积分账户（一人一户）、流水（BizId 幂等唯一索引）、规则（动作类型唯一）
        /// </summary>
        public void Configure(EntityTypeBuilder<PointAccount> builder)
        {
            builder.ToTable("PointAccounts");
            builder.HasKey(p => p.Id);

            // 一人一户：唯一索引兜底（服务层先查后建，并发冲突由索引拒绝）
            builder.HasIndex(p => p.UserId)
                .IsUnique()
                .HasDatabaseName("IX_PointAccounts_UserId");

            builder.Property(p => p.UserId).IsRequired();
            builder.Property(p => p.Balance).IsRequired();
            builder.Property(p => p.TotalEarned).IsRequired();
            builder.Property(p => p.TotalSpent).IsRequired();
            builder.Property(p => p.LastCheckinDate);
            builder.Property(p => p.ConsecutiveCheckinDays).IsRequired().HasDefaultValue(0);
        }

        /// <summary>
        /// 流水表配置：BizId 唯一索引=幂等键（同一动作重复发放冲突拒绝）；
        /// 部分唯一索引仅约束非空 BizId（系统手工调分等无幂等键场景可空）
        /// </summary>
        public void Configure(EntityTypeBuilder<PointTransaction> builder)
        {
            builder.ToTable("PointTransactions");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.UserId).IsRequired();
            builder.Property(t => t.Direction).IsRequired();
            builder.Property(t => t.GrantType).IsRequired().HasMaxLength(50);
            builder.Property(t => t.Amount).IsRequired();
            builder.Property(t => t.BalanceAfter).IsRequired();
            builder.Property(t => t.BizId).HasMaxLength(100);
            builder.Property(t => t.Remark).HasMaxLength(200);
            builder.Property(t => t.ExpireAt);

            // 幂等唯一索引：仅 BizId 非空行参与（PostgreSQL 部分索引）
            builder.HasIndex(t => t.BizId)
                .IsUnique()
                .HasFilter("\"BizId\" IS NOT NULL")
                .HasDatabaseName("UX_PointTransactions_BizId");

            // 明细页主查询：UserId + CreatedAt 倒序；日上限统计：UserId + GrantType + CreatedAt
            builder.HasIndex(t => new { t.UserId, t.CreatedAt })
                .HasDatabaseName("IX_PointTransactions_UserId_CreatedAt");
            builder.HasIndex(t => new { t.UserId, t.GrantType, t.CreatedAt })
                .HasDatabaseName("IX_PointTransactions_UserId_GrantType_CreatedAt");
        }

        /// <summary>
        /// 规则表配置：GrantType 唯一（服务层按类型取规则）
        /// </summary>
        public void Configure(EntityTypeBuilder<PointGrantRule> builder)
        {
            builder.ToTable("PointGrantRules");
            builder.HasKey(r => r.Id);

            builder.Property(r => r.GrantType).IsRequired().HasMaxLength(50);
            builder.Property(r => r.DisplayName).IsRequired().HasMaxLength(50);
            builder.Property(r => r.Amount).IsRequired();
            builder.Property(r => r.DailyLimit).IsRequired();
            builder.Property(r => r.LadderJson).HasMaxLength(200);
            builder.Property(r => r.IsEnabled).IsRequired().HasDefaultValue(true);
            builder.Property(r => r.Description).HasMaxLength(200);

            builder.HasIndex(r => r.GrantType)
                .IsUnique()
                .HasDatabaseName("UX_PointGrantRules_GrantType");
        }
    }
}
