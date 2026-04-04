using MediatR;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Application.Features.Users.Commands
{
    /// <summary>
    /// 从认证服务创建业务用户命令
    /// 当用户首次登录时，由 UserContextMiddleware 调用
    /// </summary>
    public record CreateUserFromAuthCommand : IRequest<Guid>
    {
        /// <summary>
        /// 认证系统用户ID
        /// </summary>
        public string AuthUserId { get; init; } = string.Empty;

        /// <summary>
        /// 用户类型
        /// </summary>
        public UserType UserType { get; init; } = UserType.Individual;

        /// <summary>
        /// 注册来源
        /// </summary>
        public RegistrationSource RegistrationSource { get; init; } = RegistrationSource.Web;

        /// <summary>
        /// 注册IP
        /// </summary>
        public string? RegisterIp { get; init; }

        /// <summary>
        /// 用户昵称
        /// </summary>
        public string? Nickname { get; init; }
    }
}
