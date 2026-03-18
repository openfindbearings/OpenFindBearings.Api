using MediatR;
using OpenFindBearings.Application.Features.BearingTypes.DTOs;

namespace OpenFindBearings.Application.Features.BearingTypes.Queries
{
    /// <summary>
    /// 获取单个轴承类型查询
    /// </summary>
    public record GetBearingTypeQuery(Guid Id) : IRequest<BearingTypeDto?>;
}
