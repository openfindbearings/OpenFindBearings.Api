namespace OpenFindBearings.Api.Services
{
    /// <summary>
    /// 当前用户服务
    /// 提供获取当前用户信息的统一方法
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>
        /// 当前用户ID（业务系统ID）
        /// </summary>
        Guid? UserId { get; }

        /// <summary>
        /// 当前用户认证ID
        /// </summary>
        string? AuthUserId { get; }

        /// <summary>
        /// 会话ID（游客）
        /// </summary>
        string? SessionId { get; }

        /// <summary>
        /// 是否已认证
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// 是否是游客
        /// </summary>
        bool IsGuest { get; }

        /// <summary>
        /// 用户类型
        /// </summary>
        string? UserType { get; }

        /// <summary>
        /// 客户端IP
        /// </summary>
        string? ClientIp { get; }

        /// <summary>
        /// 用户代理
        /// </summary>
        string? UserAgent { get; }
    }
}
