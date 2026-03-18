using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 用户关注商家配置类
    /// </summary>
    public class UserFollowConfiguration : IEntityTypeConfiguration<UserFollow>
    {
        public void Configure(EntityTypeBuilder<UserFollow> builder)
        {
            builder.ToTable("UserFollows");

            builder.HasKey(uf => uf.Id);

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
