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

            // 改动说明：BaseEntity 构造函数预赋了 Guid.Id，而 Npgsql 对 Guid 主键默认不启用
            // 值生成器——EF 经导航集合发现新实体时按"主键已设值=库中已存在"判定为 Modified，
            // 收藏写入变成 UPDATE 不存在行 → 并发异常整批回滚（收藏永远写不进库）。
            // 声明 OnAdd 生成后判定回归 Added→INSERT；连接表 Id 保存前无人引用，覆盖安全
            builder.Property(uf => uf.Id).ValueGeneratedOnAdd();

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
