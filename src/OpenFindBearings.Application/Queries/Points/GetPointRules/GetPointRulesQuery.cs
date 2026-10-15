using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Points.GetPointRules
{
    /// <summary>
    /// 轴承币与商家金规则数据源查询（v2.17.0）：规则页渲染用，匿名可访问，
    /// 只读查询经 IQuery 标记跳过 UnitOfWork 行为。
    /// </summary>
    public record GetPointRulesQuery : IRequest<PointRulesDto>, IQuery;
}