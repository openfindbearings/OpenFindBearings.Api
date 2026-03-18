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

            // 用户类型枚举存为字符串
            builder.Property(u => u.UserType)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(u => u.GuestSessionId)
                .HasMaxLength(100);

            builder.HasOne(u => u.Merchant)
                .WithMany(m => m.Staff)
                .HasForeignKey(u => u.MerchantId)
                .OnDelete(DeleteBehavior.Restrict);

            // 导航属性 - 角色关联
            builder.HasMany(u => u.UserRoles)
                .WithOne(ur => ur.User)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 导航属性 - 收藏的轴承
            builder.HasMany(u => u.FavoriteBearings)
                .WithOne(uf => uf.User)
                .HasForeignKey(uf => uf.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 导航属性 - 关注的商家
            builder.HasMany(u => u.FollowedMerchants)
                .WithOne(uf => uf.User)
                .HasForeignKey(uf => uf.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 导航属性 - 轴承浏览历史
            builder.HasMany(u => u.BearingHistory)
                .WithOne(ubh => ubh.User)
                .HasForeignKey(ubh => ubh.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 导航属性 - 商家浏览历史
            builder.HasMany(u => u.MerchantHistory)
                .WithOne(umh => umh.User)
                .HasForeignKey(umh => umh.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 导航属性 - 提交的纠错
            builder.HasMany(u => u.SubmittedCorrections)
                .WithOne(c => c.Submitter)
                .HasForeignKey(c => c.SubmittedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // 索引
            builder.HasIndex(u => u.Email);
            builder.HasIndex(u => u.UserType);
            builder.HasIndex(u => u.MerchantId);
            builder.HasIndex(u => u.GuestSessionId);
        }
    }
}