namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 我的意见反馈 DTO（用户端）
    /// 只暴露本人可见字段：内容、图片、状态与管理员回复，不含内部分诊信息
    /// </summary>
    public class MyFeedbackDto
    {
        /// <summary>反馈 ID</summary>
        public Guid Id { get; set; }

        /// <summary>分类枚举名（Suggestion/Bug/ContentReport/Other）</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>分类中文展示名</summary>
        public string CategoryText { get; set; } = string.Empty;

        /// <summary>反馈内容</summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>附件图片相对路径</summary>
        public List<string> ImageUrls { get; set; } = new();

        /// <summary>处理状态枚举名（Pending/Processing/Resolved/Closed）</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>处理状态中文展示名</summary>
        public string StatusText { get; set; } = string.Empty;

        /// <summary>管理员回复（可空，未回复时前端展示"处理中"）</summary>
        public string? AdminReply { get; set; }

        /// <summary>回复时间（UTC）</summary>
        public DateTime? RepliedAt { get; set; }

        /// <summary>提交时间（UTC）</summary>
        public DateTime CreatedAt { get; set; }
    }
}