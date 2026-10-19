using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// 撮合会话两表配置（v2.18.0 T1 会话）：会话容器与消息。
    /// 唯一约束 (DemandId, MerchantId)=同单同商户只有一条会话；
    /// 消息按 (ConversationId, CreatedAt) 索引支撑分页拉取与未读统计。
    /// 与寻货三表同口径：不建到 User/Merchant/Demand 的导航（只存 Id+索引），保持聚合边界干净。
    /// </summary>
    public class SourcingConversationConfiguration :
        IEntityTypeConfiguration<SourcingConversation>,
        IEntityTypeConfiguration<SourcingMessage>
    {
        /// <summary>
        /// 会话表：(DemandId, MerchantId) 唯一；需求方维度与商户维度列表索引
        /// </summary>
        public void Configure(EntityTypeBuilder<SourcingConversation> builder)
        {
            builder.ToTable("SourcingConversations");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.DemandId).IsRequired();
            builder.Property(c => c.MerchantId).IsRequired();
            builder.Property(c => c.PublisherUserId).IsRequired();
            builder.Property(c => c.PublisherMerchantId);
            builder.Property(c => c.PublisherLastReadAt);
            builder.Property(c => c.MerchantLastReadAt);
            builder.Property(c => c.LastMessageAt);
            builder.Property(c => c.LastMessagePreview).HasMaxLength(60);

            // 同单同商户唯一：会话由首条消息懒创建，唯一索引兼作并发兜底
            builder.HasIndex(c => new { c.DemandId, c.MerchantId })
                .IsUnique()
                .HasDatabaseName("UX_SourcingConversations_Demand_Merchant");
            // 需求方会话列表（我的网页 → 会话 tab）
            builder.HasIndex(c => new { c.PublisherUserId, c.LastMessageAt })
                .HasDatabaseName("IX_SourcingConversations_Publisher_LastAt");
            // 商户方会话列表（商户工作台 → 会话）
            builder.HasIndex(c => new { c.MerchantId, c.LastMessageAt })
                .HasDatabaseName("IX_SourcingConversations_Merchant_LastAt");
        }

        /// <summary>
        /// 消息表：按会话 + 时间检索（分页拉取、未读计数）
        /// </summary>
        public void Configure(EntityTypeBuilder<SourcingMessage> builder)
        {
            builder.ToTable("SourcingMessages");
            builder.HasKey(m => m.Id);

            builder.Property(m => m.ConversationId).IsRequired();
            builder.Property(m => m.SenderUserId).IsRequired();
            builder.Property(m => m.SenderSide).IsRequired();
            builder.Property(m => m.Content).IsRequired().HasMaxLength(2000);

            builder.HasIndex(m => new { m.ConversationId, m.CreatedAt })
                .HasDatabaseName("IX_SourcingMessages_Conversation_CreatedAt");
        }
    }
}
