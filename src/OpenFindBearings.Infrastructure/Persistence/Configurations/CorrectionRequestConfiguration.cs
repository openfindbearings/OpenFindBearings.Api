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

            // 目标类型和目标ID
            builder.Property(c => c.TargetType)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(c => c.TargetId)
                .IsRequired();

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
            builder.HasIndex(c => c.TargetType);
            builder.HasIndex(c => c.TargetId);
            builder.HasIndex(c => c.SubmittedBy);
            builder.HasIndex(c => new { c.TargetType, c.TargetId });

            // 关系配置 - 根据目标类型动态关联
            builder.HasOne<Bearing>()
                .WithMany()
                .HasForeignKey(c => c.TargetId)
                .HasConstraintName("FK_CorrectionRequests_Bearings_TargetId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.HasOne<Merchant>()
                .WithMany()
                .HasForeignKey(c => c.TargetId)
                .HasConstraintName("FK_CorrectionRequests_Merchants_TargetId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.HasOne(c => c.Submitter)
                .WithMany(u => u.SubmittedCorrections)
                .HasForeignKey(c => c.SubmittedBy)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Reviewer)
                .WithMany()
                .HasForeignKey(c => c.ReviewedBy)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
