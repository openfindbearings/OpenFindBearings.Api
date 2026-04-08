using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.BearingTypes.GetBearingTypeByCode
{
    /// <summary>
    /// 根据代码获取轴承类型查询
    /// </summary>
    public record GetBearingTypeByCodeQuery(string Code) : IRequest<BearingTypeDto?>, IQuery;
}
