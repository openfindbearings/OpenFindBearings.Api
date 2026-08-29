using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.MerchantBearings.GetMerchantsByBearing;

/// <summary>
/// 获取销售指定轴承的商家列表查询（反向查询：轴承 -> 商家）
/// 用于管理后台轴承列表页展示某型号轴承的多家在售商家
/// </summary>
public record GetMerchantsByBearingQuery : IRequest<PagedResult<MerchantDto>>, IQuery
{
    /// <summary>
    /// 轴承ID
    /// </summary>
    public Guid BearingId { get; init; }

    /// <summary>
    /// 是否只显示在售商家，默认true
    /// </summary>
    public bool? OnlyOnSale { get; init; } = true;

    /// <summary>
    /// 页码
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// 每页条数
    /// </summary>
    public int PageSize { get; init; } = 20;
}
