using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 用户收藏轴承配置类
    /// </summary>
    public class UserBearingFavoriteConfiguration : IEntityTypeConfiguration<UserBearingFavorite>
    {
        public void Configure(EntityTypeBuilder<UserBearingFavorite> builder)
        {
            builder.ToTable("UserBearingFavorites");

            builder.HasKey(uf => uf.Id);

            // 改动说明：曾经在此声明 Id.ValueGeneratedOnAdd() 试图让导航新增实体回归 Added，
            // 实测无效（EF 导航修复判定与它无关，且该列无数据库默认值，INSERT 会省略 Id 列）。
            // 现回滚为 Npgsql 默认值生成配置，连接表实体一律改为显式仓储 Add/Delete 写入

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

            // 添加全局过滤器，只显示活跃轴承的收藏
            builder.HasQueryFilter(ubf =>
                ubf.Bearing != null && ubf.Bearing.IsActive);
        }
    }
}
