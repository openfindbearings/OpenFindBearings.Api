using MediatR;
using OpenFindBearings.Application.Features.Mobile.DTOs;

namespace OpenFindBearings.Application.Features.Mobile.Queries
{
    /// <summary>
    /// 获取移动端配置查询
    /// </summary>
    public record GetMobileConfigQuery : IRequest<MobileConfigDto>;
}
