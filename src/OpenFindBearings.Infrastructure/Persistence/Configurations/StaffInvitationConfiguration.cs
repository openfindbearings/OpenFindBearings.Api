using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 员工邀请配置类
    /// </summary>
    public class StaffInvitationConfiguration : IEntityTypeConfiguration<StaffInvitation>
    {
        public void Configure(EntityTypeBuilder<StaffInvitation> builder)
        {
            builder.ToTable("StaffInvitations");

            builder.HasKey(i => i.Id);

            // 属性配置
            builder.Property(i => i.MerchantId)
                .IsRequired()
                .HasColumnName("MerchantId");

            builder.Property(i => i.Email)
                .HasMaxLength(200)
                .HasColumnName("Email");

            builder.Property(i => i.Phone)
                .HasMaxLength(20)
                .HasColumnName("Phone");

            builder.Property(i => i.Role)
                .HasMaxLength(50)
                .HasColumnName("Role");

            builder.Property(i => i.InvitationCode)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("InvitationCode");

            builder.Property(i => i.OperatorId)
                .IsRequired()
                .HasColumnName("OperatorId");

            builder.Property(i => i.IsCompleted)
                .HasDefaultValue(false)
                .HasColumnName("IsCompleted");

            builder.Property(i => i.CompletedSub)
                .HasMaxLength(100)
                .HasColumnName("CompletedSub");

            builder.Property(i => i.CompletedAt)
                .HasColumnName("CompletedAt");

            // 索引
            builder.HasIndex(i => i.InvitationCode)
                .IsUnique()
                .HasDatabaseName("IX_StaffInvitations_InvitationCode");

            builder.HasIndex(i => i.Email)
                .HasDatabaseName("IX_StaffInvitations_Email");

            builder.HasIndex(i => i.Phone)
                .HasDatabaseName("IX_StaffInvitations_Phone");

            builder.HasIndex(i => i.IsCompleted)
                .HasDatabaseName("IX_StaffInvitations_IsCompleted");

            builder.HasIndex(i => i.MerchantId)
                .HasDatabaseName("IX_StaffInvitations_MerchantId");
        }
    }
}
