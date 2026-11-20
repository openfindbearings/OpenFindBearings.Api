using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    public class BrandConfiguration : IEntityTypeConfiguration<Brand>
    {
        public void Configure(EntityTypeBuilder<Brand> builder)
        {
            builder.ToTable("Brands");

            builder.HasKey(b => b.Id);

            builder.Property(b => b.Code)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(b => b.Name)
                .IsRequired()
                .HasMaxLength(200);

            // 改动说明（2026-11-20 拼音检索）：与商户同口径的三列，写入时预计算
            builder.Property(b => b.SearchPinyin)
                .HasMaxLength(512);

            builder.Property(b => b.SearchInitials)
                .HasMaxLength(256);

            builder.Property(b => b.SearchPinyinFuzzy)
                .HasMaxLength(512);

            builder.Property(b => b.Country)
                .HasMaxLength(100);

            builder.Property(b => b.LogoUrl)
                .HasColumnType("text");
            // 改动说明（品牌资料完善）：新增可空资料字段，人工维护，长度与展示场景对齐
            builder.Property(b => b.Description).HasColumnType("text");
            builder.Property(b => b.ParentCompany).HasMaxLength(100);
            builder.Property(b => b.Website).HasMaxLength(200);
            builder.Property(b => b.Advantages).HasColumnType("text");
            builder.Property(b => b.SuffixNotes).HasColumnType("text");
            builder.Property(b => b.MainApplications).HasMaxLength(200);

            builder.HasIndex(b => b.Code)
                .IsUnique();

            builder.OwnsOne(b => b.DataSource, ds =>
            {
                ds.Property(d => d.SourceType)
                    .HasColumnName("DataSourceType")
                    .HasConversion(DataSourceTypeValueConverter.Instance)
                    .HasMaxLength(50);

                ds.Property(d => d.ImportedBy)
                    .HasColumnName("ImportedBy")
                    .HasMaxLength(100);

                ds.Property(d => d.ImportedAt)
                    .HasColumnName("ImportedAt");
            });

            builder.HasQueryFilter(b => b.IsActive);
        }
    }
}
