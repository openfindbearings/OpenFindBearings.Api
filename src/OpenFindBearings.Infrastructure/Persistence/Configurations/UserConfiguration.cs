using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.Id);

            // ============ 关联字段 ============
            builder.Property(u => u.AuthUserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.HasIndex(u => u.AuthUserId)
                .IsUnique();

            // ============ 基础信息 ============
            builder.Property(u => u.Nickname)
                .HasMaxLength(100);

            builder.Property(u => u.Avatar)
                .HasMaxLength(500);

            builder.Property(u => u.Address)
                .HasMaxLength(500);

            // 用户类型枚举存为字符串
            builder.Property(u => u.UserType)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(u => u.GuestSessionId)
                .HasMaxLength(100);

            // ============ 会员信息 ============
            builder.Property(u => u.Level)
                .HasConversion<int>()
                .HasDefaultValue(UserLevel.Free);

            builder.Property(u => u.SubscriptionExpiry);

            // ============ 注册信息 ============
            builder.Property(u => u.RegistrationSource)
                .HasConversion<int>()
                .HasDefaultValue(RegistrationSource.Guest);

            builder.Property(u => u.RegisterIp)
                .HasMaxLength(50);

            builder.Property(u => u.RegisteredAt);

            // ============ 用户画像 ============
            builder.Property(u => u.Occupation)
                .HasConversion<int?>();

            builder.Property(u => u.CompanyName)
                .HasMaxLength(200);

            builder.Property(u => u.Industry)
                .HasMaxLength(100);

            // ============ 行为统计 ============
            builder.Property(u => u.SearchCount)
                .HasDefaultValue(0);

            builder.Property(u => u.QueryCount)
                .HasDefaultValue(0);

            builder.Property(u => u.FirstSearchAt);
            builder.Property(u => u.LastSearchAt);
            builder.Property(u => u.LastActiveAt);
            builder.Property(u => u.LastLoginAt);

            // ============ 状态字段 ============
            builder.Property(u => u.IsActive)
                .HasDefaultValue(true);

            builder.Property(u => u.IsMerged)
                .HasDefaultValue(false);

            builder.Property(u => u.MergedToUserId);

            // ============ 关系配置 ============
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

            // ============ 索引 ============
            builder.HasIndex(u => u.UserType);
            builder.HasIndex(u => u.MerchantId);
            builder.HasIndex(u => u.GuestSessionId);

            builder.HasIndex(u => u.Level);
            builder.HasIndex(u => u.RegistrationSource);
            builder.HasIndex(u => u.IsActive);
            builder.HasIndex(u => u.LastActiveAt);

            // 组合索引
            builder.HasIndex(u => new { u.UserType, u.IsActive });
            builder.HasIndex(u => new { u.Level, u.IsActive });
        }
    }
}