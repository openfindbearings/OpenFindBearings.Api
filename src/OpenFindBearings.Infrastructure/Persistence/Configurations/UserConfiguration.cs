using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.AuthUserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.HasIndex(u => u.AuthUserId)
                .IsUnique();

            builder.Property(u => u.Nickname)
                .HasMaxLength(100);

            builder.Property(u => u.Avatar)
                .HasMaxLength(500);

            builder.Property(u => u.Email)
                .HasMaxLength(200);

            builder.Property(u => u.Phone)
                .HasMaxLength(20);

            // 用户类型枚举存为字符串（更可读）
            builder.Property(u => u.UserType)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.HasOne(u => u.Merchant)
                .WithMany(m => m.Staff)  // 需要在 Merchant 类中添加 Staff 集合
                .HasForeignKey(u => u.MerchantId)
                .OnDelete(DeleteBehavior.Restrict);

            // 移除 Customer 相关配置

            // 游客会话ID（可选）
            builder.Property(u => u.GuestSessionId)
                .HasMaxLength(100);

            builder.Property(u => u.CreatedAt)
                .IsRequired();

            builder.Property(u => u.LastLoginAt);
        }
    }
}