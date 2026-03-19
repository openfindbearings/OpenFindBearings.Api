using MediatR;
using OpenFindBearings.Application.Features.Mobile.DTOs;

namespace OpenFindBearings.Application.Features.Mobile.Queries
{
    /// <summary>
    /// 获取移动端首页查询
    /// </summary>
    public record GetMobileHomeQuery : IRequest<MobileHomeDto>
    {
        public bool IsAuthenticated { get; init; }
        public Guid? UserId { get; init; }
    }
}
