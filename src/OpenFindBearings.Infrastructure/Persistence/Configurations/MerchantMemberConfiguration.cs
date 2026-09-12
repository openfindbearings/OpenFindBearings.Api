using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 商户成员表配置
    /// </summary>
    public class MerchantMemberConfiguration : IEntityTypeConfiguration<MerchantMember>
    {
        public void Configure(EntityTypeBuilder<MerchantMember> builder)
        {
            builder.ToTable("MerchantMembers");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.UserId)
                .IsRequired()
                .HasColumnName("UserId");

            builder.Property(m => m.MerchantId)
                .IsRequired()
                .HasColumnName("MerchantId");

            builder.Property(m => m.Role)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("Role");

            builder.Property(m => m.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(MerchantMemberStatus.Active)
                .HasColumnName("Status");

            builder.Property(m => m.InvitedBy)
                .HasColumnName("InvitedBy");

            builder.Property(m => m.JoinedAt)
                .IsRequired()
                .HasColumnName("JoinedAt");

            builder.Property(m => m.RemovedAt)
                .HasColumnName("RemovedAt");

            // 一人一商户一条成员记录（复用 Removed 行）
            builder.HasIndex(m => new { m.UserId, m.MerchantId })
                .IsUnique()
                .HasDatabaseName("IX_MerchantMembers_User_Merchant");

            builder.HasIndex(m => m.UserId)
                .HasDatabaseName("IX_MerchantMembers_UserId");

            builder.HasIndex(m => m.MerchantId)
                .HasDatabaseName("IX_MerchantMembers_MerchantId");

            builder.HasIndex(m => m.Status)
                .HasDatabaseName("IX_MerchantMembers_Status");

            // 关系：用户/商户（删除受限，成员关系不能被级联清除）
            builder.HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.Merchant)
                .WithMany()
                .HasForeignKey(m => m.MerchantId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
