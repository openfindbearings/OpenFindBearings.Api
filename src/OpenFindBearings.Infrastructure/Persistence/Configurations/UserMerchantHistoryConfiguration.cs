using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 用户商家浏览历史配置类
    /// </summary>
    public class UserMerchantHistoryConfiguration : IEntityTypeConfiguration<UserMerchantHistory>
    {
        public void Configure(EntityTypeBuilder<UserMerchantHistory> builder)
        {
            builder.ToTable("UserMerchantHistories");

            builder.HasKey(umh => umh.Id);

            builder.Property(umh => umh.ViewedAt)
                .IsRequired();

            builder.HasIndex(umh => umh.UserId);
            builder.HasIndex(umh => umh.MerchantId);
            builder.HasIndex(umh => umh.ViewedAt);

            builder.HasOne(umh => umh.User)
                .WithMany(u => u.MerchantHistory)
                .HasForeignKey(umh => umh.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(umh => umh.Merchant)
                .WithMany()
                .HasForeignKey(umh => umh.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
