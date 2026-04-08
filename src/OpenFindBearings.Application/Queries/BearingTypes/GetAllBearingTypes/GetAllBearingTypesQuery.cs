using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.BearingTypes.GetAllBearingTypes
{
    /// <summary>
    /// 获取所有轴承类型列表查询
    /// </summary>
    public record GetAllBearingTypesQuery : IRequest<List<BearingTypeDto>>, IQuery
    {

    }
}
