using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 站内信通知表配置
    /// </summary>
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications");

            builder.HasKey(n => n.Id);

            builder.Property(n => n.UserId)
                .IsRequired()
                .HasColumnName("UserId");

            builder.Property(n => n.Type)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("Type");

            builder.Property(n => n.Title)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("Title");

            builder.Property(n => n.Body)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnName("Body");

            builder.Property(n => n.BizType)
                .HasMaxLength(50)
                .HasColumnName("BizType");

            builder.Property(n => n.BizId)
                .HasColumnName("BizId");

            builder.Property(n => n.IsRead)
                .IsRequired()
                .HasDefaultValue(false)
                .HasColumnName("IsRead");

            builder.Property(n => n.ReadAt)
                .HasColumnName("ReadAt");

            // 收件人列表主查询路径：UserId + CreatedAt 倒序；未读角标走 UserId + IsRead
            builder.HasIndex(n => new { n.UserId, n.CreatedAt })
                .HasDatabaseName("IX_Notifications_UserId_CreatedAt");

            builder.HasIndex(n => new { n.UserId, n.IsRead })
                .HasDatabaseName("IX_Notifications_UserId_IsRead");
        }
    }
}
