using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Bearings.GetBearingInterchanges
{
    /// <summary>
    /// 获取轴承替代品查询（v2.16.0：返回含三级类型与方向的替代条目）
    /// </summary>
    public record GetBearingInterchangesQuery : IRequest<List<BearingInterchangeDto>>, IQuery
    {
        /// <summary>
        /// 轴承Id
        /// </summary>
        public Guid BearingId { get; set; }
    }
}
