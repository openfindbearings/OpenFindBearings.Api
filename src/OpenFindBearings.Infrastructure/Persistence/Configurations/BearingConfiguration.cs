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

            // 值对象配置 - Dimensions
            builder.OwnsOne(b => b.Dimensions, dim =>
            {
                dim.Property(d => d.InnerDiameter)
                    .HasColumnName("InnerDiameter")
                    .HasPrecision(10, 3)
                    .IsRequired();

                dim.Property(d => d.OuterDiameter)
                    .HasColumnName("OuterDiameter")
                    .HasPrecision(10, 3)
                    .IsRequired();

                dim.Property(d => d.Width)
                    .HasColumnName("Width")
                    .HasPrecision(10, 3)
                    .IsRequired();

                // 在值对象配置内部创建索引
                dim.HasIndex(d => d.InnerDiameter)
                    .HasDatabaseName("IX_Bearings_InnerDiameter");

                dim.HasIndex(d => d.OuterDiameter)
                    .HasDatabaseName("IX_Bearings_OuterDiameter");

                dim.HasIndex(d => d.Width)
                    .HasDatabaseName("IX_Bearings_Width");
            });

            // 值对象配置 - Performance
            builder.OwnsOne(b => b.Performance, perf =>
            {
                perf.Property(p => p.DynamicLoadRating)
                    .HasColumnName("DynamicLoadRating")
                    .HasPrecision(10, 2);

                perf.Property(p => p.StaticLoadRating)
                    .HasColumnName("StaticLoadRating")
                    .HasPrecision(10, 2);

                perf.Property(p => p.LimitingSpeed)
                    .HasColumnName("LimitingSpeed");
            });

            builder.Property(b => b.Weight)
                .HasPrecision(10, 3);

            builder.Property(b => b.PrecisionGrade)
                .HasMaxLength(20);

            builder.Property(b => b.Material)
                .HasMaxLength(50);

            builder.Property(b => b.SealType)
                .HasMaxLength(50);

            builder.Property(b => b.CageType)
                .HasMaxLength(50);

            builder.Property(b => b.ViewCount)
                .HasDefaultValue(0);

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

            builder.HasIndex(b => b.BearingTypeId)
                .HasDatabaseName("IX_Bearings_BearingTypeId");

            builder.HasIndex(b => b.BrandId)
                .HasDatabaseName("IX_Bearings_BrandId");
        }
    }
}
