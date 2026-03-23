using MediatR;
using OpenFindBearings.Application.Features.Users.DTOs;

namespace OpenFindBearings.Application.Features.Users.Queries
{
    /// <summary>
    /// 根据会话ID获取游客用户查询
    /// </summary>
    public record GetUserBySessionIdQuery : IRequest<UserDto?>
    {
        public string SessionId { get; init; } = string.Empty;
    }
}
