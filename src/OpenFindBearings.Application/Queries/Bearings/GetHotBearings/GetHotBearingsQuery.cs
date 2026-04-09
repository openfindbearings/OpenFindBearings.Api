using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Bearings.GetHotBearings
{
    /// <summary>
    /// 获取热门轴承查询
    /// </summary>
    public record GetHotBearingsQuery : IRequest<List<BearingDto>>, IQuery
    {
        /// <summary>
        /// 数量
        /// </summary>
        public int Count { get; set; }
    }
}
