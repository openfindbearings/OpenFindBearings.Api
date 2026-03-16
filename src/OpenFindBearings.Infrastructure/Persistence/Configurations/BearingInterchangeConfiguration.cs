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

            builder.Property(bi => bi.InterchangeType)
                .HasMaxLength(20);

            builder.Property(bi => bi.Source)
                .HasMaxLength(100);

            // 索引
            builder.HasIndex(bi => bi.SourceBearingId);
            builder.HasIndex(bi => bi.TargetBearingId);
            builder.HasIndex(bi => new { bi.SourceBearingId, bi.TargetBearingId }).IsUnique();

            // 关系配置
            builder.HasOne(bi => bi.SourceBearing)
                .WithMany()
                .HasForeignKey(bi => bi.SourceBearingId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(bi => bi.TargetBearing)
                .WithMany()
                .HasForeignKey(bi => bi.TargetBearingId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
