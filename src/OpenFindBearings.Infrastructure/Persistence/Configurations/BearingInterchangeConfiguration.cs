using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    public class BearingInterchangeConfiguration : IEntityTypeConfiguration<BearingInterchange>
    {
        public void Configure(EntityTypeBuilder<BearingInterchange> builder)
        {
            builder.ToTable("BearingInterchanges");

            builder.HasKey(bi => bi.Id);

            // 配置属性
            builder.Property(bi => bi.InterchangeType)
                .HasMaxLength(20)
                .HasDefaultValue("exact");

            builder.Property(bi => bi.Source)
                .HasMaxLength(100);

            builder.Property(bi => bi.Remarks)
                .HasMaxLength(500);

            builder.Property(bi => bi.Confidence)
                .HasDefaultValue(80);

            builder.Property(bi => bi.IsBidirectional)
                .HasDefaultValue(true);

            // 创建唯一索引：确保同一对轴承只有一个替代关系
            builder.HasIndex(bi => new { bi.SourceBearingId, bi.TargetBearingId })
                .IsUnique()
                .HasDatabaseName("IX_BearingInterchanges_Source_Target");

            // 创建单列索引优化查询
            builder.HasIndex(bi => bi.SourceBearingId)
                .HasDatabaseName("IX_BearingInterchanges_SourceBearingId");

            builder.HasIndex(bi => bi.TargetBearingId)
                .HasDatabaseName("IX_BearingInterchanges_TargetBearingId");

            // ============ 关键配置：关系映射 ============

            // 关系1: SourceBearing -> SourceInterchanges
            // 明确指定使用 SourceInterchanges 导航属性
            builder.HasOne(bi => bi.SourceBearing)
                .WithMany(b => b.SourceInterchanges)  // 明确指向 SourceInterchanges
                .HasForeignKey(bi => bi.SourceBearingId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(true);  // 源轴承是必需的

            // 关系2: TargetBearing -> TargetInterchanges
            // 明确指定使用 TargetInterchanges 导航属性
            builder.HasOne(bi => bi.TargetBearing)
                .WithMany(b => b.TargetInterchanges)  // 明确指向 TargetInterchanges
                .HasForeignKey(bi => bi.TargetBearingId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(true);  // 目标轴承是必需的

            // 可选：添加全局过滤器确保不返回已删除轴承的替代关系
            builder.HasQueryFilter(bi =>
                bi.SourceBearing != null && bi.SourceBearing.IsActive &&
                bi.TargetBearing != null && bi.TargetBearing.IsActive);
        }
    }
}
