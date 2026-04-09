using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Users.GetUserBySessionId
{
    /// <summary>
    /// 根据会话ID获取游客用户查询
    /// </summary>
    public record GetUserBySessionIdQuery : IRequest<UserDto?>, IQuery
    {
        public string SessionId { get; init; } = string.Empty;
    }
}
