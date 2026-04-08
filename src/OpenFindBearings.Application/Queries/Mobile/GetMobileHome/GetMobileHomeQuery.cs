using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Mobile.GetMobileHome
{
    /// <summary>
    /// 获取移动端首页查询
    /// </summary>
    public record GetMobileHomeQuery : IRequest<MobileHomeDto>, IQuery
    {
        public bool IsAuthenticated { get; init; }
        public Guid? UserId { get; init; }
    }
}
