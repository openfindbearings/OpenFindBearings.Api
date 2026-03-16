using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    public class BearingConfiguration : IEntityTypeConfiguration<Bearing>
    {
        public void Configure(EntityTypeBuilder<Bearing> builder)
        {
            builder.ToTable("Bearings");

            builder.HasKey(b => b.Id);

            builder.Property(b => b.PartNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(b => b.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(b => b.Dimensions.InnerDiameter)
                .HasPrecision(10, 3);

            builder.Property(b => b.Dimensions.OuterDiameter)
                .HasPrecision(10, 3);

            builder.Property(b => b.Dimensions.Width)
                .HasPrecision(10, 3);

            builder.Property(b => b.Weight)
                .HasPrecision(10, 3);

            builder.HasOne(b => b.BearingType)
                .WithMany()
                .HasForeignKey(b => b.BearingTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(b => b.Brand)
                .WithMany()
                .HasForeignKey(b => b.BrandId)
                .OnDelete(DeleteBehavior.Restrict);

            // 全局查询过滤器
            builder.HasQueryFilter(b => b.IsActive);

            // 组合唯一索引：型号 + 品牌ID
            builder.HasIndex(b => new { b.PartNumber, b.BrandId })
                .IsUnique()
                .HasDatabaseName("IX_Bearings_PartNumber_BrandId");

            builder.HasIndex(b => b.BearingTypeId);
            builder.HasIndex(b => b.BrandId);
            builder.HasIndex(b => b.Dimensions.InnerDiameter);
            builder.HasIndex(b => b.Dimensions.OuterDiameter);
        }
    }
}
