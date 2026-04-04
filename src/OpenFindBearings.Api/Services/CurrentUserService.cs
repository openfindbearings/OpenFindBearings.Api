using OpenFindBearings.Api.Helpers;

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

    /// <summary>
    /// 当前用户服务实现
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? UserId => _httpContextAccessor.HttpContext?.GetUserId();

        public string? AuthUserId => _httpContextAccessor.HttpContext?.GetAuthUserId();

        public string? SessionId => _httpContextAccessor.HttpContext?.GetSessionId();

        public bool IsAuthenticated => !string.IsNullOrEmpty(AuthUserId);

        public bool IsGuest => _httpContextAccessor.HttpContext?.Items["IsGuest"] as bool? ?? false;

        public string? UserType => _httpContextAccessor.HttpContext?.Items["UserType"] as string;

        public string? ClientIp => _httpContextAccessor.HttpContext?.GetClientIp();

        public string? UserAgent => _httpContextAccessor.HttpContext?.GetUserAgent();
    }
}
