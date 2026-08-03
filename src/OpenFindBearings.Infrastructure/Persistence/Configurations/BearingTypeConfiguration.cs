using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    public class BearingTypeConfiguration : IEntityTypeConfiguration<BearingType>
    {
        public void Configure(EntityTypeBuilder<BearingType> builder)
        {
            builder.ToTable("BearingTypes");

            builder.HasKey(bt => bt.Id);

            builder.Property(bt => bt.Code)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(bt => bt.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(bt => bt.Description)
                .HasColumnType("text");

            builder.HasIndex(bt => bt.Code)
                .IsUnique();

            builder.OwnsOne(bt => bt.DataSource, ds =>
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

            builder.HasQueryFilter(bt => bt.IsActive);
        }
    }
}
