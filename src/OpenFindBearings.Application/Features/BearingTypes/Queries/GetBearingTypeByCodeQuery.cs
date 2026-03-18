using MediatR;
using OpenFindBearings.Application.Features.BearingTypes.DTOs;

namespace OpenFindBearings.Application.Features.BearingTypes.Queries
{
    /// <summary>
    /// 根据代码获取轴承类型查询
    /// </summary>
    public record GetBearingTypeByCodeQuery(string Code) : IRequest<BearingTypeDto?>;
}
