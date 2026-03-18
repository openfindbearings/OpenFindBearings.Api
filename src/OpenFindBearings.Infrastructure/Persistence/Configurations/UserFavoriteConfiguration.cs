using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 用户收藏轴承配置类
    /// </summary>
    public class UserFavoriteConfiguration : IEntityTypeConfiguration<UserBearingFavorite>
    {
        public void Configure(EntityTypeBuilder<UserBearingFavorite> builder)
        {
            builder.ToTable("UserFavorites");

            builder.HasKey(uf => uf.Id);

            builder.HasIndex(uf => new { uf.UserId, uf.BearingId })
                .IsUnique()
                .HasDatabaseName("IX_UserFavorites_UserId_BearingId");

            builder.HasIndex(uf => uf.UserId);
            builder.HasIndex(uf => uf.BearingId);

            builder.HasOne(uf => uf.User)
                .WithMany(u => u.FavoriteBearings)
                .HasForeignKey(uf => uf.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(uf => uf.Bearing)
                .WithMany(b => b.FavoritedByUsers)
                .HasForeignKey(uf => uf.BearingId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
