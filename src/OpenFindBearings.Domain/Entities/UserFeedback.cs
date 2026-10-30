using OpenFindBearings.Domain.Abstractions;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 用户意见反馈实体
    /// 记录用户通过"意见反馈"入口提交的建议、问题与举报，供管理员分诊并做一次性回复；
    /// 图片只存已上传的相对媒体键（/uploads/feedback/...），不接受外链，避免内容注入与图片失效；
    /// 回复后由领域事件向提交人发站内信，用户可在"我的反馈"查看结果
    /// </summary>
    public class UserFeedback : BaseEntity
    {
        /// <summary>反馈内容最大长度</summary>
        public const int MaxContentLength = 1000;

        /// <summary>反馈内容最小长度（过滤"好""测试"这类无信息量提交）</summary>
        public const int MinContentLength = 5;

        /// <summary>附件图片张数上限</summary>
        public const int MaxImages = 3;

        /// <summary>联系方式最大长度</summary>
        public const int MaxContactLength = 100;

        /// <summary>客户端信息最大长度（机型/版本，便于排障）</summary>
        public const int MaxClientInfoLength = 200;

        /// <summary>管理员回复最大长度</summary>
        public const int MaxReplyLength = 500;

        /// <summary>图片相对路径前缀，提交时据此校验来源合法性</summary>
        public const string ImagePathPrefix = "/uploads/feedback/";

        /// <summary>提交人用户 ID</summary>
        public Guid UserId { get; private set; }

        /// <summary>反馈分类</summary>
        public FeedbackCategory Category { get; private set; }

        /// <summary>反馈内容</summary>
        public string Content { get; private set; } = string.Empty;

        /// <summary>附件图片相对路径（最多 3 张）</summary>
        public string[] ImageUrls { get; private set; } = [];

        /// <summary>用户主动填写的联系方式（可空，便于需要时回访）</summary>
        public string? Contact { get; private set; }

        /// <summary>客户端信息（平台与版本，取自请求头，可空）</summary>
        public string? ClientInfo { get; private set; }

        /// <summary>处理状态（管理端分诊用）</summary>
        public FeedbackStatus Status { get; private set; } = FeedbackStatus.Pending;

        /// <summary>管理员回复内容（可空，回复后不可再改，避免用户侧展示与通知不一致）</summary>
        public string? AdminReply { get; private set; }

        /// <summary>回复时间（UTC）</summary>
        public DateTime? RepliedAt { get; private set; }

        /// <summary>回复的管理员用户 ID</summary>
        public Guid? RepliedByUserId { get; private set; }

        /// <summary>
        /// 私有构造函数，仅供 EF Core 使用
        /// </summary>
        private UserFeedback() { }

        /// <summary>
        /// 创建一条用户反馈
        /// </summary>
        /// <param name="userId">提交人用户 ID</param>
        /// <param name="category">反馈分类</param>
        /// <param name="content">反馈内容（去首尾空格后写入）</param>
        /// <param name="imageUrls">附件图片相对路径，可空</param>
        /// <param name="contact">联系方式，可空</param>
        /// <param name="clientInfo">客户端信息，可空</param>
        public UserFeedback(
            Guid userId,
            FeedbackCategory category,
            string content,
            IEnumerable<string>? imageUrls,
            string? contact,
            string? clientInfo)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("提交人不能为空", nameof(userId));

            var trimmed = (content ?? string.Empty).Trim();
            if (trimmed.Length < MinContentLength || trimmed.Length > MaxContentLength)
                throw new ArgumentException($"反馈内容长度须在 {MinContentLength} 到 {MaxContentLength} 字之间", nameof(content));

            var images = (imageUrls ?? Enumerable.Empty<string>())
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Select(u => u.Trim())
                .ToList();
            if (images.Count > MaxImages)
                throw new ArgumentException($"最多上传 {MaxImages} 张图片", nameof(imageUrls));

            UserId = userId;
            Category = category;
            Content = trimmed;
            ImageUrls = images.ToArray();
            Contact = string.IsNullOrWhiteSpace(contact) ? null : contact.Trim();
            ClientInfo = string.IsNullOrWhiteSpace(clientInfo)
                ? null
                : clientInfo.Trim().Length > MaxClientInfoLength
                    ? clientInfo.Trim()[..MaxClientInfoLength]
                    : clientInfo.Trim();
            Status = FeedbackStatus.Pending;
        }

        /// <summary>
        /// 变更处理状态（管理端分诊流转）
        /// 回复过的反馈不允许再退回待处理，避免用户侧已看到回复却又显示"处理中"
        /// </summary>
        /// <param name="status">目标状态</param>
        public void SetStatus(FeedbackStatus status)
        {
            if (AdminReply != null && status == FeedbackStatus.Pending)
                throw new InvalidOperationException("已回复的反馈不能退回待处理");

            Status = status;
            UpdateTimestamp();
        }

        /// <summary>
        /// 管理员回复：写入回复内容并把状态置为已处理
        /// 一次性回复，重复回复会覆盖旧内容（管理员提交前前端二次确认）
        /// </summary>
        /// <param name="adminUserId">回复的管理员用户 ID</param>
        /// <param name="reply">回复内容</param>
        public void Reply(Guid adminUserId, string reply)
        {
            var trimmed = (reply ?? string.Empty).Trim();
            if (trimmed.Length == 0 || trimmed.Length > MaxReplyLength)
                throw new ArgumentException($"回复内容须在 1 到 {MaxReplyLength} 字之间", nameof(reply));

            AdminReply = trimmed;
            RepliedAt = DateTime.UtcNow;
            RepliedByUserId = adminUserId;
            Status = FeedbackStatus.Resolved;
            UpdateTimestamp();

            // 改动说明（v2.22.1）：回复后抛事件，订阅者在事务提交后向提交人发站内信
            AddDomainEvent(new Events.FeedbackRepliedEvent(Id, UserId, trimmed));
        }
    }
}
