using MediatR;
using OpenFindBearings.Application.Features.BearingTypes.DTOs;

namespace OpenFindBearings.Application.Features.BearingTypes.Queries
{
    /// <summary>
    /// 获取所有轴承类型列表查询
    /// </summary>
    public record GetAllBearingTypesQuery : IRequest<List<BearingTypeDto>>;
}
