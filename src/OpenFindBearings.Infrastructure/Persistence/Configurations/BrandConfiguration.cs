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

            builder.Property(b => b.Country)
                .HasMaxLength(100);

            builder.Property(b => b.LogoUrl)
                .HasColumnType("text");

            builder.HasIndex(b => b.Code)
                .IsUnique();

            builder.OwnsOne(b => b.DataSource, ds =>
            {
                ds.Property(d => d.SourceType)
                    .HasColumnName("DataSourceType")
                    .HasConversion<string>()
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
