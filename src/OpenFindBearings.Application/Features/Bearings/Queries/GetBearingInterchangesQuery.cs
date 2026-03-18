using MediatR;
using OpenFindBearings.Application.Features.Bearings.DTOs;

namespace OpenFindBearings.Application.Features.Bearings.Queries
{
    /// <summary>
    /// 获取轴承替代品查询
    /// </summary>
    public record GetBearingInterchangesQuery(Guid BearingId) : IRequest<List<BearingDto>>;
}
