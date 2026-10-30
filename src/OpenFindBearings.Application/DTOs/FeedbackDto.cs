namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 意见反馈 DTO（管理端）
    /// 带提交人昵称与脱敏手机号，便于管理员在分诊时辨别来源
    /// </summary>
    public class FeedbackDto
    {
        /// <summary>反馈 ID</summary>
        public Guid Id { get; set; }

        /// <summary>提交人用户 ID</summary>
        public Guid UserId { get; set; }

        /// <summary>提交人昵称（可空）</summary>
        public string? UserNickname { get; set; }

        /// <summary>提交人手机号（脱敏）</summary>
        public string? UserMobile { get; set; }

        /// <summary>分类枚举名（Suggestion/Bug/ContentReport/Other）</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>分类中文展示名</summary>
        public string CategoryText { get; set; } = string.Empty;

        /// <summary>反馈内容</summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>附件图片相对路径</summary>
        public List<string> ImageUrls { get; set; } = new();

        /// <summary>用户填写的联系方式（可空）</summary>
        public string? Contact { get; set; }

        /// <summary>客户端信息（平台与版本，可空）</summary>
        public string? ClientInfo { get; set; }

        /// <summary>处理状态枚举名（Pending/Processing/Resolved/Closed）</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>处理状态中文展示名</summary>
        public string StatusText { get; set; } = string.Empty;

        /// <summary>管理员回复（可空）</summary>
        public string? AdminReply { get; set; }

        /// <summary>回复时间（UTC）</summary>
        public DateTime? RepliedAt { get; set; }

        /// <summary>提交时间（UTC）</summary>
        public DateTime CreatedAt { get; set; }
    }
}