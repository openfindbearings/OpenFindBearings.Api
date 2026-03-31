using OpenFindBearings.Api.Helpers;

namespace OpenFindBearings.Api.Services
{
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
