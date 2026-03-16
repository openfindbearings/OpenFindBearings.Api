using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    public class CorrectionRequestConfiguration : IEntityTypeConfiguration<CorrectionRequest>
    {
        public void Configure(EntityTypeBuilder<CorrectionRequest> builder)
        {
            builder.ToTable("CorrectionRequests");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.EntityType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(c => c.FieldName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.SuggestedValue)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(c => c.OriginalValue)
                .HasMaxLength(500);

            builder.Property(c => c.Reason)
                .HasMaxLength(1000);

            builder.Property(c => c.ReviewComment)
                .HasMaxLength(500);

            // 将枚举存为字符串
            builder.Property(c => c.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            // 索引
            builder.HasIndex(c => c.Status);
            builder.HasIndex(c => c.EntityType);
            builder.HasIndex(c => c.SubmittedBy);
        }
    }
}
