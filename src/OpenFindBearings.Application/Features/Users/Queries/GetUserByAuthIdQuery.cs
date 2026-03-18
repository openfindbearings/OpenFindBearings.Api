using MediatR;
using OpenFindBearings.Application.Features.Users.DTOs;

namespace OpenFindBearings.Application.Features.Users.Queries
{
    /// <summary>
    /// 根据认证ID获取用户查询
    /// </summary>
    public record GetUserByAuthIdQuery(string AuthUserId) : IRequest<UserDto?>;
}
