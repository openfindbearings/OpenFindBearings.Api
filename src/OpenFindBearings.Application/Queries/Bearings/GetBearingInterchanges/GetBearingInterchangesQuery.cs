using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Bearings.GetBearingInterchanges
{
    /// <summary>
    /// 获取轴承替代品查询
    /// </summary>
    public record GetBearingInterchangesQuery : IRequest<List<BearingDto>>, IQuery
    {
        /// <summary>
        /// 轴承Id
        /// </summary>
        public Guid BearingId { get; set; }
    }
}
