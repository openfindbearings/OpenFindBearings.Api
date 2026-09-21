using OpenFindBearings.Api.Extensions;

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
        /// 当前用户手机号（JWT phone_number claim，v2.9.0 邀请确认制服务端比对用）
        /// </summary>
        string? Phone { get; }

        /// <summary>
        /// 当前用户邮箱（JWT email claim，v2.9.0 员工邀请无手机号时的兜底匹配）
        /// </summary>
        string? Email { get; }

        /// <summary>
        /// 租户ID（从 JWT token claims 中读取）
        /// </summary>
        Guid? TenantId { get; }

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
        /// 当前商户上下文（商户级操作定位用，X-Merchant-Id 或首个在职成员商户）
        /// </summary>
        Guid? CurrentMerchantId { get; }

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

        /// <inheritdoc/>
        public Guid? UserId => _httpContextAccessor.HttpContext?.GetUserId();

        /// <inheritdoc/>
        public string? AuthUserId => _httpContextAccessor.HttpContext?.GetAuthUserId();

        /// <inheritdoc/>
        public string? Phone => _httpContextAccessor.HttpContext?.GetPhone();

        /// <inheritdoc/>
        public string? Email => _httpContextAccessor.HttpContext?.GetEmail();

        /// <inheritdoc/>
        public Guid? TenantId => _httpContextAccessor.HttpContext?.GetTenantId();

        /// <inheritdoc/>
        public string? SessionId => _httpContextAccessor.HttpContext?.GetSessionId();

        /// <inheritdoc/>
        public bool IsAuthenticated => !string.IsNullOrEmpty(AuthUserId);

        /// <inheritdoc/>
        public bool IsGuest => _httpContextAccessor.HttpContext?.Items["IsGuest"] as bool? ?? false;

        /// <inheritdoc/>
        public string? UserType => _httpContextAccessor.HttpContext?.Items["UserType"] as string;

        /// <inheritdoc/>
        public Guid? CurrentMerchantId
        {
            get
            {
                var items = _httpContextAccessor.HttpContext?.Items;
                return items != null && items["CurrentMerchantId"] is Guid merchantId ? merchantId : null;
            }
        }

        /// <inheritdoc/>
        public string? ClientIp => _httpContextAccessor.HttpContext?.GetClientIp();

        /// <inheritdoc/>
        public string? UserAgent => _httpContextAccessor.HttpContext?.GetUserAgent();
    }
}
