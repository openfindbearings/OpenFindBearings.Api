using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    public class BearingConfiguration : IEntityTypeConfiguration<Bearing>
    {
        public void Configure(EntityTypeBuilder<Bearing> builder)
        {
            builder.ToTable("Bearings");

            // ============ 主键 ============
            builder.HasKey(b => b.Id);

            // ============ 基本属性 ============
            builder.Property(b => b.CurrentCode)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("CurrentCode");

            builder.Property(b => b.FormerCode)
                .HasMaxLength(100)
                .HasColumnName("FormerCode");

            builder.Property(b => b.CodeSource)
                .HasMaxLength(50)
                .HasColumnName("CodeSource");

            builder.Property(b => b.Name)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("Name");

            builder.Property(b => b.Description)
                .HasMaxLength(1000)
                .HasColumnName("Description");

            // ============ 类型属性 ============
            builder.Property(b => b.BearingType)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("BearingType");

            builder.Property(b => b.StructureType)
                .HasMaxLength(100)
                .HasColumnName("StructureType");

            builder.Property(b => b.SizeSeries)
                .HasMaxLength(50)
                .HasColumnName("SizeSeries");

            builder.Property(b => b.IsStandard)
                .IsRequired()
                .HasDefaultValue(true)
                .HasColumnName("IsStandard");

            // ============ 值对象 - Dimensions ============
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

                // 索引
                dim.HasIndex(d => d.InnerDiameter)
                    .HasDatabaseName("IX_Bearings_InnerDiameter");

                dim.HasIndex(d => d.OuterDiameter)
                    .HasDatabaseName("IX_Bearings_OuterDiameter");

                dim.HasIndex(d => d.Width)
                    .HasDatabaseName("IX_Bearings_Width");
            });

            // ============ 倒角尺寸 ============
            builder.Property(b => b.ChamferRmin)
                .HasPrecision(8, 2)
                .HasColumnName("ChamferRmin");

            builder.Property(b => b.ChamferRmax)
                .HasPrecision(8, 2)
                .HasColumnName("ChamferRmax");

            // ============ 重量 ============
            builder.Property(b => b.Weight)
                .HasPrecision(12, 4)
                .HasColumnName("Weight");

            // ============ 技术参数 ============
            builder.Property(b => b.PrecisionGrade)
                .HasMaxLength(10)
                .HasColumnName("PrecisionGrade");

            builder.Property(b => b.Material)
                .HasMaxLength(50)
                .HasColumnName("Material");

            builder.Property(b => b.SealType)
                .HasMaxLength(20)
                .HasColumnName("SealType");

            builder.Property(b => b.CageType)
                .HasMaxLength(50)
                .HasColumnName("CageType");

            // ============ 值对象 - Performance ============
            builder.OwnsOne(b => b.Performance, perf =>
            {
                perf.Property(p => p.HasData)
                    .HasColumnName("PerformanceHasData")
                    .HasDefaultValue(false)
                    .IsRequired();

                perf.Property(p => p.DynamicLoadRating)
                    .HasColumnName("DynamicLoadRating")
                    .HasPrecision(12, 2);

                perf.Property(p => p.StaticLoadRating)
                    .HasColumnName("StaticLoadRating")
                    .HasPrecision(12, 2);

                perf.Property(p => p.LimitingSpeed)
                    .HasColumnName("LimitingSpeed")
                    .HasPrecision(10, 0);
            });

            // ============ 产地和商标 ============
            builder.Property(b => b.OriginCountry)
                .HasMaxLength(50)
                .HasColumnName("OriginCountry");

            builder.Property(b => b.Trademark)
                .HasMaxLength(100)
                .HasColumnName("Trademark");

            // ============ 产品类别 ============
            builder.Property(b => b.Category)
                .HasConversion<int>()
                .HasDefaultValue(BearingCategory.Domestic)
                .HasSentinel(BearingCategory.Unknown)
                .HasColumnName("Category");

            // ============ 值对象 - DataSource ============
            builder.OwnsOne(b => b.DataSource, ds =>
            {
                ds.Property(d => d.SourceType)
                    .HasColumnName("DataSourceType")
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired(false);

                ds.Property(d => d.CrawlerSite)
                    .HasColumnName("CrawlerSite")
                    .HasConversion<int?>();

                ds.Property(d => d.SourceUrl)
                    .HasColumnName("SourceUrl")
                    .HasMaxLength(1000);

                ds.Property(d => d.SourceDetail)
                    .HasColumnName("SourceDetail")
                    .HasMaxLength(500);

                ds.Property(d => d.SourceId)
                    .HasColumnName("SourceId")
                    .HasMaxLength(200);

                ds.Property(d => d.ImportedBy)
                    .HasColumnName("ImportedBy")
                    .HasMaxLength(100);

                ds.Property(d => d.ImportedAt)
                    .HasColumnName("ImportedAt");

                ds.Property(d => d.ReliabilityScore)
                    .HasColumnName("ReliabilityScore");

                // 数据来源索引
                ds.HasIndex(d => d.SourceType)
                    .HasDatabaseName("IX_Bearings_DataSourceType");
            });

            // ============ 数据追溯字段 ============
            builder.Property(b => b.LastVerifiedAt)
                .HasColumnName("LastVerifiedAt");

            builder.Property(b => b.VerifiedBy)
                .HasMaxLength(100)
                .HasColumnName("VerifiedBy");

            builder.Property(b => b.IsVerified)
                .HasDefaultValue(false)
                .HasColumnName("IsVerified");

            builder.Property(b => b.DataRemark)
                .HasMaxLength(2000)
                .HasColumnName("DataRemark");

            // ============ 统计字段 ============
            builder.Property(b => b.ViewCount)
                .HasDefaultValue(0)
                .HasColumnName("ViewCount");

            // ============ 关联配置 ============
            // 关联轴承类型
            builder.HasOne(b => b.BearingTypeNavigation)
                .WithMany()
                .HasForeignKey(b => b.BearingTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // 关联品牌
            builder.HasOne(b => b.Brand)
                .WithMany()
                .HasForeignKey(b => b.BrandId)
                .OnDelete(DeleteBehavior.Restrict);

            // ============ 导航属性配置（一对多） ============
            // MerchantBearings - Bearing 作为父级
            builder.HasMany(b => b.MerchantBearings)
                .WithOne()
                .HasForeignKey(mb => mb.BearingId)
                .OnDelete(DeleteBehavior.Cascade);

            // SourceInterchanges - 源轴承
            builder.HasMany(b => b.SourceInterchanges)
                .WithOne()
                .HasForeignKey(i => i.SourceBearingId)
                .OnDelete(DeleteBehavior.Cascade);

            // TargetInterchanges - 目标轴承
            builder.HasMany(b => b.TargetInterchanges)
                .WithOne()
                .HasForeignKey(i => i.TargetBearingId)
                .OnDelete(DeleteBehavior.Restrict);

            // FavoritedByUsers
            builder.HasMany(b => b.FavoritedByUsers)
                .WithOne(f => f.Bearing)
                .HasForeignKey(f => f.BearingId)
                .OnDelete(DeleteBehavior.Cascade);

            // ============ 索引 ============
            // 组合索引：型号 + 品牌ID
            builder.HasIndex(b => new { b.CurrentCode, b.BrandId })
                .IsUnique()
                .HasDatabaseName("IX_Bearings_CurrentCode_BrandId");

            // 单列索引
            builder.HasIndex(b => b.BearingType)
                .HasDatabaseName("IX_Bearings_BearingType");

            builder.HasIndex(b => b.BearingTypeId)
                .HasDatabaseName("IX_Bearings_BearingTypeId");

            builder.HasIndex(b => b.StructureType)
                .HasDatabaseName("IX_Bearings_StructureType");

            builder.HasIndex(b => b.SizeSeries)
                .HasDatabaseName("IX_Bearings_SizeSeries");

            builder.HasIndex(b => b.IsStandard)
                .HasDatabaseName("IX_Bearings_IsStandard");

            builder.HasIndex(b => b.BrandId)
                .HasDatabaseName("IX_Bearings_BrandId");

            builder.HasIndex(b => b.Category)
                .HasDatabaseName("IX_Bearings_Category");

            builder.HasIndex(b => b.IsVerified)
                .HasDatabaseName("IX_Bearings_IsVerified");

            // 组合索引：常用查询组合
            builder.HasIndex(b => new { b.BearingType, b.IsStandard })
                .HasDatabaseName("IX_Bearings_Type_IsStandard");

            builder.HasIndex(b => new { b.CurrentCode, b.BearingType })
                .HasDatabaseName("IX_Bearings_Code_Type");

            // ============ 全局查询过滤器 ============
            // 如果需要软删除，请添加 IsActive 字段
            builder.HasQueryFilter(b => b.IsActive);
        }
    }
}
