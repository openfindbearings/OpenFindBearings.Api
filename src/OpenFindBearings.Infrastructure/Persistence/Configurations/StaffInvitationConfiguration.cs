using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    public class StaffInvitationConfiguration : IEntityTypeConfiguration<StaffInvitation>
    {
        public void Configure(EntityTypeBuilder<StaffInvitation> builder)
        {
            builder.ToTable("StaffInvitations");

            builder.HasKey(i => i.Id);

            builder.Property(i => i.MerchantId).IsRequired();
            builder.Property(i => i.Email).HasMaxLength(200);
            builder.Property(i => i.Phone).HasMaxLength(20);
            builder.Property(i => i.Role).HasMaxLength(50);
            builder.Property(i => i.InvitationCode).IsRequired().HasMaxLength(50);
            builder.Property(i => i.OperatorId).IsRequired();
            builder.Property(i => i.CompletedSub).HasMaxLength(100);

            builder.HasIndex(i => i.InvitationCode).IsUnique();
            builder.HasIndex(i => i.Email);
            builder.HasIndex(i => i.Phone);
            builder.HasIndex(i => i.IsCompleted);
        }
    }
}
