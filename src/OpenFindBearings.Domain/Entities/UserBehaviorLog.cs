using OpenFindBearings.Domain.Abstractions;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 用户行为日志
    /// </summary>
    public class UserBehaviorLog : BaseEntity
    {
        /// <summary>
        /// 用户ID（可为空，游客用SessionId标识）
        /// </summary>
        public Guid? UserId { get; private set; }

        /// <summary>
        /// 游客会话ID
        /// </summary>
        public string? SessionId { get; private set; }

        /// <summary>
        /// 行为类型（Search/View/Click/Favorite/Share）
        /// </summary>
        public string ActionType { get; private set; } = string.Empty;

        /// <summary>
        /// 目标类型（Bearing/Merchant/Brand）
        /// </summary>
        public string TargetType { get; private set; } = string.Empty;

        /// <summary>
        /// 目标ID
        /// </summary>
        public Guid? TargetId { get; private set; }

        /// <summary>
        /// 搜索关键词（如果是搜索行为）
        /// </summary>
        public string? Keyword { get; private set; }

        /// <summary>
        /// 客户端IP
        /// </summary>
        public string? ClientIp { get; private set; }

        /// <summary>
        /// 用户代理
        /// </summary>
        public string? UserAgent { get; private set; }

        /// <summary>
        /// 请求耗时（毫秒）
        /// </summary>
        public int? DurationMs { get; private set; }

        /// <summary>
        /// 额外数据（JSON格式）
        /// </summary>
        public string? ExtraData { get; private set; }

        private UserBehaviorLog() { }

        public UserBehaviorLog(
            Guid? userId,
            string? sessionId,
            string actionType,
            string targetType,
            Guid? targetId = null,
            string? keyword = null,
            string? clientIp = null,
            string? userAgent = null,
            int? durationMs = null,
            string? extraData = null)
        {
            UserId = userId;
            SessionId = sessionId;
            ActionType = actionType;
            TargetType = targetType;
            TargetId = targetId;
            Keyword = keyword;
            ClientIp = clientIp;
            UserAgent = userAgent;
            DurationMs = durationMs;
            ExtraData = extraData;
        }
    }
}
