using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    public class LicenseVerificationConfiguration : IEntityTypeConfiguration<LicenseVerification>
    {
        public void Configure(EntityTypeBuilder<LicenseVerification> builder)
        {
            builder.ToTable("LicenseVerifications");

            builder.HasKey(l => l.Id);

            builder.Property(l => l.LicenseUrl)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(l => l.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(l => l.ReviewComment)
                .HasMaxLength(500);

            builder.HasIndex(l => l.Status);
            builder.HasIndex(l => l.MerchantId);
            builder.HasIndex(l => l.SubmittedAt);

            builder.HasOne(l => l.Merchant)
                .WithMany()
                .HasForeignKey(l => l.MerchantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(l => l.Submitter)
                .WithMany()
                .HasForeignKey(l => l.SubmittedBy)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(l => l.Reviewer)
                .WithMany()
                .HasForeignKey(l => l.ReviewedBy)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
