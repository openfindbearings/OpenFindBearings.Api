using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 用户意见反馈实体配置
    /// 图片列表按 text[] 存储（与商家商品图同口径），并建立管理端分诊与用户侧查询所需索引
    /// </summary>
    public class UserFeedbackConfiguration : IEntityTypeConfiguration<UserFeedback>
    {
        /// <summary>
        /// 配置表名、字段长度、数组映射与索引
        /// </summary>
        public void Configure(EntityTypeBuilder<UserFeedback> builder)
        {
            builder.ToTable("UserFeedbacks");

            builder.HasKey(f => f.Id);

            builder.Property(f => f.UserId)
                .IsRequired();

            builder.Property(f => f.Category)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(f => f.Content)
                .IsRequired()
                .HasMaxLength(UserFeedback.MaxContentLength);

            // 图片列表 text[]：默认空数组，避免插入时出现 null 元素集合
            builder.Property(f => f.ImageUrls)
                .HasColumnType("text[]")
                .HasDefaultValue(Array.Empty<string>());

            builder.Property(f => f.Contact)
                .HasMaxLength(UserFeedback.MaxContactLength);

            builder.Property(f => f.ClientInfo)
                .HasMaxLength(UserFeedback.MaxClientInfoLength);

            builder.Property(f => f.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(f => f.AdminReply)
                .HasMaxLength(UserFeedback.MaxReplyLength);

            // 管理端默认按"状态 + 时间倒序"分诊，用户侧按"提交人 + 时间倒序"查看
            builder.HasIndex(f => new { f.Status, f.CreatedAt });

            builder.HasIndex(f => new { f.UserId, f.CreatedAt });
        }
    }
}