using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Admin.GetDashboardStats
{
    /// <summary>
    /// 获取仪表盘统计数据查询
    /// </summary>
    public record GetDashboardStatsQuery : IRequest<DashboardStatsDto>, IQuery
    {
    }
}
