using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Repositories
{
    /// <summary>
    /// 意见反馈查询参数（管理端分诊列表）
    /// </summary>
    public class FeedbackSearchParams
    {
        /// <summary>
        /// 处理状态筛选（空=全部）
        /// </summary>
        public FeedbackStatus? Status { get; set; }

        /// <summary>
        /// 分类筛选（空=全部）
        /// </summary>
        public FeedbackCategory? Category { get; set; }

        /// <summary>
        /// 关键词（模糊匹配反馈内容与联系方式）
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 页码
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// 每页条数
        /// </summary>
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// 意见反馈列表项读模型
    /// 在仓储层一次性带出提交人昵称与手机号，避免应用层逐条回查用户造成 N+1
    /// </summary>
    public class FeedbackListItem
    {
        /// <summary>反馈 ID</summary>
        public Guid Id { get; set; }

        /// <summary>提交人用户 ID</summary>
        public Guid UserId { get; set; }

        /// <summary>提交人昵称（可空）</summary>
        public string? UserNickname { get; set; }

        /// <summary>提交人手机号（脱敏后展示）</summary>
        public string? UserMobile { get; set; }

        /// <summary>分类</summary>
        public FeedbackCategory Category { get; set; }

        /// <summary>反馈内容</summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>附件图片相对路径</summary>
        public string[] ImageUrls { get; set; } = [];

        /// <summary>联系方式（可空）</summary>
        public string? Contact { get; set; }

        /// <summary>客户端信息（可空）</summary>
        public string? ClientInfo { get; set; }

        /// <summary>处理状态</summary>
        public FeedbackStatus Status { get; set; }

        /// <summary>管理员回复（可空）</summary>
        public string? AdminReply { get; set; }

        /// <summary>回复时间（UTC）</summary>
        public DateTime? RepliedAt { get; set; }

        /// <summary>提交时间（UTC）</summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 意见反馈仓储接口
    /// 负责提交落库、用户侧计数与列表、管理端分诊列表与状态流转
    /// </summary>
    public interface IUserFeedbackRepository
    {
        /// <summary>
        /// 新增反馈（仅加入变更跟踪，提交由 UnitOfWork 负责）
        /// </summary>
        Task AddAsync(UserFeedback feedback, CancellationToken cancellationToken = default);

        /// <summary>
        /// 按 ID 获取反馈
        /// </summary>
        Task<UserFeedback?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新反馈（仅标记为已修改，提交由 UnitOfWork 负责）
        /// </summary>
        Task UpdateAsync(UserFeedback feedback, CancellationToken cancellationToken = default);

        /// <summary>
        /// 统计指定用户在给定时间点之后提交的反馈条数（按业务日界计数用于每日限频）
        /// </summary>
        Task<int> CountSinceAsync(Guid userId, DateTime sinceUtc, CancellationToken cancellationToken = default);

        /// <summary>
        /// 分页查询指定用户自己的反馈（按提交时间倒序）
        /// </summary>
        Task<PagedResult<UserFeedback>> SearchByUserAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// 管理端分页查询反馈（带提交人昵称与脱敏手机号，按提交时间倒序）
        /// </summary>
        Task<PagedResult<FeedbackListItem>> SearchAsync(FeedbackSearchParams searchParams, CancellationToken cancellationToken = default);
    }
}