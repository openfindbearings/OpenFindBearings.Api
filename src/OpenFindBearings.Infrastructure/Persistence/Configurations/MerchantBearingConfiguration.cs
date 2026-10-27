using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;

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

            // v2.15.0 状态统一：SaleState + ReviewStatus 取代 IsOnSale/IsRestocking/IsPendingApproval/IsFeatured
            // 不设 DB 默认值：CLR 默认(None/OffShelf) 是合法业务态（仓库中），设默认会导致实体写入被默认值覆盖
            builder.Property(mp => mp.SaleState)
                .HasConversion<int>();

            builder.Property(mp => mp.ReviewStatus)
                .HasConversion<int>();

            builder.Property(mp => mp.ApprovalComment)
                .HasMaxLength(500);

            builder.Property(mp => mp.DataSourceType)
                .HasConversion<int>();

            // v2.22.0 商家商品实拍图：text[] 数组列，首个元素为首图；默认空数组便于存量行无需回填
            builder.Property(mp => mp.ImageUrls)
                .HasColumnType("text[]")
                .HasDefaultValue(Array.Empty<string>());

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
            builder.HasIndex(mp => mp.SaleState);
            builder.HasIndex(mp => mp.ReviewStatus);
            builder.HasIndex(mp => new { mp.MerchantId, mp.BearingId }).IsUnique();

            // 添加全局过滤器，只显示活跃轴承的商家关联
            builder.HasQueryFilter(mb =>
                mb.Bearing != null && mb.Bearing.IsActive);
        }
    }
}
