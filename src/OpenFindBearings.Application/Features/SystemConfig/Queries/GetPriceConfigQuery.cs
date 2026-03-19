using MediatR;
using OpenFindBearings.Application.Features.SystemConfig.DTOs;

namespace OpenFindBearings.Application.Features.SystemConfig.Queries
{
    /// <summary>
    /// 获取价格配置查询
    /// </summary>
    public record GetPriceConfigQuery : IRequest<PriceConfigDto>;
}
