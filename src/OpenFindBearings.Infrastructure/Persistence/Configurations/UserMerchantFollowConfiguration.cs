using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 用户关注商家配置类
    /// </summary>
    public class UserMerchantFollowConfiguration : IEntityTypeConfiguration<UserMerchantFollow>
    {
        public void Configure(EntityTypeBuilder<UserMerchantFollow> builder)
        {
            builder.ToTable("UserMerchantFollows");

            builder.HasKey(uf => uf.Id);

            // 改动说明：同 UserBearingFavoriteConfiguration——Npgsql Guid 主键默认无值生成器，
            // 导航发现的新关注实体被判 Modified → UPDATE 不存在行 → 整批回滚。
            // 声明 OnAdd 生成使其回归 Added→INSERT
            builder.Property(uf => uf.Id).ValueGeneratedOnAdd();

            builder.HasIndex(uf => new { uf.UserId, uf.MerchantId })
                .IsUnique()
                .HasDatabaseName("IX_UserFollows_UserId_MerchantId");

            builder.HasIndex(uf => uf.UserId);
            builder.HasIndex(uf => uf.MerchantId);

            builder.HasOne(uf => uf.User)
                .WithMany(u => u.FollowedMerchants)
                .HasForeignKey(uf => uf.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(uf => uf.Merchant)
                .WithMany(m => m.FollowedByUsers)
                .HasForeignKey(uf => uf.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
