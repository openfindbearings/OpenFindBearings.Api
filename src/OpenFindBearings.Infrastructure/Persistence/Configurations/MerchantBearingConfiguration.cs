using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    public class MerchantBearingConfiguration : IEntityTypeConfiguration<MerchantBearing>
    {
        public void Configure(EntityTypeBuilder<MerchantBearing> builder)
        {
            builder.ToTable("MerchantProducts");

            builder.HasKey(mp => mp.Id);

            builder.Property(mp => mp.PriceDescription)
                .HasMaxLength(100);

            builder.Property(mp => mp.StockDescription)
                .HasMaxLength(100);

            builder.Property(mp => mp.MinOrderDescription)
                .HasMaxLength(100);

            builder.Property(mp => mp.Remarks)
                .HasMaxLength(500);

            // 关系配置
            builder.HasOne(mp => mp.Merchant)
                .WithMany()
                .HasForeignKey(mp => mp.MerchantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(mp => mp.Bearing)
                .WithMany()
                .HasForeignKey(mp => mp.BearingId)
                .OnDelete(DeleteBehavior.Restrict);

            // 索引
            builder.HasIndex(mp => mp.MerchantId);
            builder.HasIndex(mp => mp.BearingId);
            builder.HasIndex(mp => new { mp.MerchantId, mp.BearingId }).IsUnique();
        }
    }
}
