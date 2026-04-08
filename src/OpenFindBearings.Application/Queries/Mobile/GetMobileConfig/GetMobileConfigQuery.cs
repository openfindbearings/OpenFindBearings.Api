using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Mobile.GetMobileConfig
{
    /// <summary>
    /// 获取移动端配置查询
    /// </summary>
    public record GetMobileConfigQuery : IRequest<MobileConfigDto>, IQuery;
}
