using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 商城配置（v2.3.0 商城虚拟权益）：商品 Key 唯一索引（履约策略与幂等前缀锚点）、
    /// 订单按 UserId+CreatedAt 复合索引（我的订单分页主查询路径）
    /// </summary>
    public class MallConfiguration :
        IEntityTypeConfiguration<MallItem>,
        IEntityTypeConfiguration<MallOrder>
    {
        /// <summary>
        /// 配置商城商品（Key 唯一，价格/闪购窗口/库存/上下架）
        /// </summary>
        public void Configure(EntityTypeBuilder<MallItem> builder)
        {
            builder.ToTable("MallItems");
            builder.HasKey(i => i.Id);

            builder.HasIndex(i => i.Key)
                .IsUnique()
                .HasDatabaseName("UX_MallItems_Key");

            builder.Property(i => i.Key).IsRequired().HasMaxLength(64);
            builder.Property(i => i.Name).IsRequired().HasMaxLength(64);
            builder.Property(i => i.Description).IsRequired().HasMaxLength(512);
            // v2.4.0 商家挂礼：Icon 兼作礼品图片的对象存储相对路径，放宽到 256
            builder.Property(i => i.Icon).IsRequired().HasMaxLength(256);
            builder.Property(i => i.Category).IsRequired();
            builder.Property(i => i.AuditRemark).HasMaxLength(256);

            // v2.4.0：商家礼品按归属商户查询（商户"我的礼品"+Admin 待审队列）
            builder.HasIndex(i => new { i.OwnerMerchantId, i.AuditState })
                .HasDatabaseName("IX_MallItems_Owner_Audit");
        }

        /// <summary>
        /// 配置商城订单（快照字段 + 状态 + 履约目标引用）
        /// </summary>
        public void Configure(EntityTypeBuilder<MallOrder> builder)
        {
            builder.ToTable("MallOrders");
            builder.HasKey(o => o.Id);

            // 我的订单分页主查询路径（UserId 过滤 + CreatedAt 倒序）
            builder.HasIndex(o => new { o.UserId, o.CreatedAt })
                .HasDatabaseName("IX_MallOrders_User_Created");

            builder.Property(o => o.ItemKey).IsRequired().HasMaxLength(64);
            builder.Property(o => o.ItemName).IsRequired().HasMaxLength(64);
            builder.Property(o => o.Remark).HasMaxLength(256);
            builder.Property(o => o.Status).IsRequired();
        }
    }
}
