using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    public class MerchantBearingConfiguration : IEntityTypeConfiguration<MerchantBearing>
    {
        public void Configure(EntityTypeBuilder<MerchantBearing> builder)
        {
            builder.ToTable("MerchantBearings");

            builder.HasKey(mp => mp.Id);

            builder.Property(mp => mp.PriceDescription)
                .HasMaxLength(100);

            builder.Property(mp => mp.StockDescription)
                .HasMaxLength(100);

            builder.Property(mp => mp.MinOrderDescription)
                .HasMaxLength(100);

            builder.Property(mp => mp.Remarks)
                .HasMaxLength(500);

            builder.Property(mp => mp.ViewCount)
                .HasDefaultValue(0);

            builder.Property(mp => mp.IsFeatured)
                .HasDefaultValue(false);

            builder.Property(mp => mp.IsOnSale)
                .HasDefaultValue(true);

            builder.Property(mp => mp.IsPendingApproval)
                .HasDefaultValue(false);

            builder.Property(mp => mp.ApprovalComment)
                .HasMaxLength(500);

            // 关系配置
            builder.HasOne(mp => mp.Merchant)
                .WithMany(m => m.MerchantBearings)
                .HasForeignKey(mp => mp.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(mp => mp.Bearing)
                .WithMany(b => b.MerchantBearings)
                .HasForeignKey(mp => mp.BearingId)
                .OnDelete(DeleteBehavior.Cascade);

            // 索引
            builder.HasIndex(mp => mp.MerchantId);
            builder.HasIndex(mp => mp.BearingId);
            builder.HasIndex(mp => mp.IsOnSale);
            builder.HasIndex(mp => mp.IsPendingApproval);
            builder.HasIndex(mp => new { mp.MerchantId, mp.BearingId }).IsUnique();

            // 添加全局过滤器，只显示活跃轴承的商家关联
            builder.HasQueryFilter(mb =>
                mb.Bearing != null && mb.Bearing.IsActive);
        }
    }
}
