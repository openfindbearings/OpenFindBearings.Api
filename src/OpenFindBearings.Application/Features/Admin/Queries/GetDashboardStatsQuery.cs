using MediatR;
using OpenFindBearings.Application.Features.Admin.DTOs;

namespace OpenFindBearings.Application.Features.Admin.Queries
{
    /// <summary>
    /// 获取仪表盘统计数据查询
    /// </summary>
    public record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;
}
