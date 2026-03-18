using MediatR;
using OpenFindBearings.Application.Features.Bearings.DTOs;

namespace OpenFindBearings.Application.Features.Bearings.Queries
{
    /// <summary>
    /// 获取热门轴承查询
    /// </summary>
    public record GetHotBearingsQuery(int Count) : IRequest<List<BearingDto>>;
}
