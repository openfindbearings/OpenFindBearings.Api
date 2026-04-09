using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.SystemConfig.GetPriceConfig
{
    /// <summary>
    /// 获取价格配置查询
    /// </summary>
    public record GetPriceConfigQuery : IRequest<PriceConfigDto>, IQuery;
}
