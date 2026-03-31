using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 用户轴承浏览历史配置类
    /// </summary>
    public class UserBearingHistoryConfiguration : IEntityTypeConfiguration<UserBearingHistory>
    {
        public void Configure(EntityTypeBuilder<UserBearingHistory> builder)
        {
            builder.ToTable("UserBearingHistories");

            builder.HasKey(ubh => ubh.Id);

            builder.Property(ubh => ubh.ViewedAt)
                .IsRequired();

            builder.HasIndex(ubh => ubh.UserId);
            builder.HasIndex(ubh => ubh.BearingId);
            builder.HasIndex(ubh => ubh.ViewedAt);

            builder.HasOne(ubh => ubh.User)
                .WithMany(u => u.BearingHistory)
                .HasForeignKey(ubh => ubh.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ubh => ubh.Bearing)
                .WithMany()
                .HasForeignKey(ubh => ubh.BearingId)
                .OnDelete(DeleteBehavior.Cascade);

            // 添加全局过滤器，只显示活跃轴承的浏览历史
            builder.HasQueryFilter(ubh =>
                ubh.Bearing != null && ubh.Bearing.IsActive);
        }
    }
}
