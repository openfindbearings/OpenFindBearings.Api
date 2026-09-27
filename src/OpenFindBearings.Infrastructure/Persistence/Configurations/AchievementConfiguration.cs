using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 成就表配置（v2.1.0 成就子系统）：定义表 Key 唯一；解锁表 (Scope,OwnerId,AchievementKey)
    /// 唯一索引=幂等防线（同主体同成就仅一行）
    /// </summary>
    public class AchievementConfiguration :
        IEntityTypeConfiguration<AchievementDefinition>,
        IEntityTypeConfiguration<AchievementUnlock>
    {
        /// <summary>
        /// 配置成就定义（Key 唯一索引）
        /// </summary>
        public void Configure(EntityTypeBuilder<AchievementDefinition> builder)
        {
            builder.ToTable("AchievementDefinitions");
            builder.HasKey(a => a.Id);

            builder.HasIndex(a => a.Key)
                .IsUnique()
                .HasDatabaseName("UX_AchievementDefinitions_Key");

            builder.Property(a => a.Key).IsRequired().HasMaxLength(64);
            builder.Property(a => a.Name).IsRequired().HasMaxLength(64);
            builder.Property(a => a.Description).IsRequired().HasMaxLength(256);
            builder.Property(a => a.Icon).IsRequired().HasMaxLength(64);
            // 勋章图片相对媒体键（v2.6.0）：可空，键名含时间戳建议 128 长度
            builder.Property(a => a.ImageKey).HasMaxLength(128);
            builder.Property(a => a.Category).IsRequired().HasMaxLength(32);
            builder.Property(a => a.MetricKey).IsRequired().HasMaxLength(64);
            builder.Property(a => a.TitleReward).HasMaxLength(64);
            // 改动说明（v2.8.0 G11）：限量标志与窗口序号（默认非限量）
            builder.Property(a => a.IsLimited).IsRequired().HasDefaultValue(false);
            builder.Property(a => a.LimitedOrdinal);
        }

        /// <summary>
        /// 配置成就解锁（一表双轨唯一索引）
        /// </summary>
        public void Configure(EntityTypeBuilder<AchievementUnlock> builder)
        {
            builder.ToTable("AchievementUnlocks");
            builder.HasKey(a => a.Id);

            // 幂等防线：同主体同成就仅一行
            builder.HasIndex(a => new { a.Scope, a.OwnerId, a.AchievementKey })
                .IsUnique()
                .HasDatabaseName("UX_AchievementUnlocks_Owner_Key");

            builder.Property(a => a.AchievementKey).IsRequired().HasMaxLength(64);
        }
    }
}
