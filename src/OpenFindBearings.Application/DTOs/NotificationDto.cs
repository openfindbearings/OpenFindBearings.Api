namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 站内信 DTO：消息中心列表项
    /// </summary>
    public class NotificationDto
    {
        /// <summary>通知 ID</summary>
        public Guid Id { get; set; }

        /// <summary>类型（merchant_approved/merchant_rejected/nomination_accepted/system）</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>标题</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>正文</summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>业务类型（merchant/nomination，决定前端跳转；null 无跳转）</summary>
        public string? BizType { get; set; }

        /// <summary>业务对象 ID</summary>
        public Guid? BizId { get; set; }

        /// <summary>是否已读</summary>
        public bool IsRead { get; set; }

        /// <summary>创建时间（UTC，展示层转换本地时区）</summary>
        public DateTime CreatedAt { get; set; }
    }
}
