using MediatR;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Events
{
    /// <summary>
    /// 用户注册事件
    /// 当用户首次登录时触发（认证服务创建用户后通知业务服务）
    /// </summary>
    public class UserRegisteredEvent : INotification
    {
        /// <summary>
        /// 业务用户ID
        /// </summary>
        public Guid UserId { get; }

        /// <summary>
        /// 认证系统用户ID
        /// </summary>
        public string AuthUserId { get; }

        /// <summary>
        /// 注册来源
        /// </summary>
        public RegistrationSource Source { get; }

        /// <summary>
        /// 注册IP
        /// </summary>
        public string? RegisterIp { get; }

        /// <summary>
        /// 注册时间
        /// </summary>
        public DateTime OccurredAt { get; }

        public UserRegisteredEvent(Guid userId, string authUserId, RegistrationSource source, string? registerIp = null)
        {
            UserId = userId;
            AuthUserId = authUserId;
            Source = source;
            RegisterIp = registerIp;
            OccurredAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 用户登录事件
    /// 每次用户登录成功时触发
    /// </summary>
    public class UserLoggedInEvent : INotification
    {
        /// <summary>
        /// 业务用户ID
        /// </summary>
        public Guid UserId { get; }

        /// <summary>
        /// 认证系统用户ID
        /// </summary>
        public string AuthUserId { get; }

        /// <summary>
        /// 登录方式（手机号密码/验证码/微信）
        /// </summary>
        public string LoginMethod { get; }

        /// <summary>
        /// 登录IP
        /// </summary>
        public string? LoginIp { get; }

        /// <summary>
        /// 用户代理
        /// </summary>
        public string? UserAgent { get; }

        /// <summary>
        /// 登录时间
        /// </summary>
        public DateTime OccurredAt { get; }

        public UserLoggedInEvent(Guid userId, string authUserId, string loginMethod,
            string? loginIp = null, string? userAgent = null)
        {
            UserId = userId;
            AuthUserId = authUserId;
            LoginMethod = loginMethod;
            LoginIp = loginIp;
            UserAgent = userAgent;
            OccurredAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 用户登出事件
    /// </summary>
    public class UserLoggedOutEvent : INotification
    {
        public Guid UserId { get; }
        public string AuthUserId { get; }
        public DateTime OccurredAt { get; }

        public UserLoggedOutEvent(Guid userId, string authUserId)
        {
            UserId = userId;
            AuthUserId = authUserId;
            OccurredAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 用户升级事件（免费→付费）
    /// </summary>
    public class UserUpgradedEvent : INotification
    {
        public Guid UserId { get; }
        public UserLevel OldLevel { get; }
        public UserLevel NewLevel { get; }
        public DateTime ExpiryAt { get; }
        public DateTime OccurredAt { get; }

        public UserUpgradedEvent(Guid userId, UserLevel oldLevel, UserLevel newLevel, DateTime expiryAt)
        {
            UserId = userId;
            OldLevel = oldLevel;
            NewLevel = newLevel;
            ExpiryAt = expiryAt;
            OccurredAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 用户画像更新事件
    /// </summary>
    public class UserProfileUpdatedEvent : INotification
    {
        public Guid UserId { get; }
        public UserOccupation? Occupation { get; }
        public string? CompanyName { get; }
        public string? Industry { get; }
        public DateTime OccurredAt { get; }

        public UserProfileUpdatedEvent(Guid userId, UserOccupation? occupation,
            string? companyName = null, string? industry = null)
        {
            UserId = userId;
            Occupation = occupation;
            CompanyName = companyName;
            Industry = industry;
            OccurredAt = DateTime.UtcNow;
        }
    }
}
