using MediatR;
using OpenFindBearings.Application.Features.Users.DTOs;

namespace OpenFindBearings.Application.Features.Users.Queries
{
    /// <summary>
    /// 获取当前用户信息查询
    /// </summary>
    public record GetCurrentUserQuery : IRequest<UserDto>;
}
