using OpenFindBearings.Domain.Abstractions;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 撮合会话消息（v2.18.0 T1 会话）：会话内单条文本消息。
    /// 发送方以"方"（需求方/商家）标识，配合 SenderUserId 记录经办人；
    /// 内容为纯文本（平台不允许在此通道传递结构化报价，报价仍走应答标书，防止绕过比价口径）。
    /// </summary>
    public class SourcingMessage : BaseEntity
    {
        /// <summary>所属会话标识</summary>
        public Guid ConversationId { get; private set; }

        /// <summary>发送人用户标识（审计与经办人展示）</summary>
        public Guid SenderUserId { get; private set; }

        /// <summary>发送方（需求方方 / 商家方）</summary>
        public SourcingSenderSide SenderSide { get; private set; }

        /// <summary>消息正文（纯文本，最长 2000 字）</summary>
        public string Content { get; private set; } = string.Empty;

        /// <summary>供 EF Core 使用的无参构造</summary>
        private SourcingMessage() { }

        /// <summary>
        /// 构造一条消息
        /// </summary>
        /// <param name="conversationId">所属会话标识</param>
        /// <param name="senderUserId">发送人用户标识</param>
        /// <param name="senderSide">发送方</param>
        /// <param name="content">消息正文（调用方需已裁剪校验非空）</param>
        public SourcingMessage(Guid conversationId, Guid senderUserId, SourcingSenderSide senderSide, string content)
        {
            ConversationId = conversationId;
            SenderUserId = senderUserId;
            SenderSide = senderSide;
            Content = content;
        }
    }
}
