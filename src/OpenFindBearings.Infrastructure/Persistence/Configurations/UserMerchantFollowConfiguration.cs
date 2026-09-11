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

            // 改动说明：同 UserBearingFavoriteConfiguration——曾声明 ValueGeneratedOnAdd() 无效且埋雷，
            // 已回滚为 Npgsql 默认值生成配置，关注实体一律显式仓储 Add/Delete 写入

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
