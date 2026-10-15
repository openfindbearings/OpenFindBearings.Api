using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.MerchantBearings.GetMerchantsByBearing;

/// <summary>
/// 获取销售指定轴承的商家列表处理器
/// 经商家轴承关联仓储按轴承反查商家，并按商家ID去重、可选在售筛选、内存分页
/// </summary>
public class GetMerchantsByBearingQueryHandler : IRequestHandler<GetMerchantsByBearingQuery, PagedResult<BearingMerchantDto>>
{
    private readonly IMerchantBearingRepository _merchantBearingRepository;
    private readonly ILogger<GetMerchantsByBearingQueryHandler> _logger;

    public GetMerchantsByBearingQueryHandler(
        IMerchantBearingRepository merchantBearingRepository,
        ILogger<GetMerchantsByBearingQueryHandler> logger)
    {
        _merchantBearingRepository = merchantBearingRepository;
        _logger = logger;
    }

    public async Task<PagedResult<BearingMerchantDto>> Handle(
        GetMerchantsByBearingQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("获取轴承在售商家: BearingId={BearingId}, OnlyOnSale={OnlyOnSale}",
            request.BearingId, request.OnlyOnSale);

        // 改动说明：按轴承反查所有商家轴承关联（含 Merchant 导航），再据此得到在售商家列表
        var relations = await _merchantBearingRepository.GetByBearingAsync(request.BearingId, cancellationToken);

        // 改动说明（v2.3.0 商城置顶卡）：置顶判定统一用本次查询的同一 now，避免同页不同商家口径漂移
        var now = DateTime.UtcNow;

        // v2.15.0 状态统一：在售口径 = 审核通过且在售/补货中（仅已下架与未审商品的商家被排除）
        var merchants = relations
            .Where(mb => !request.OnlyOnSale.HasValue || !request.OnlyOnSale.Value
                || (mb.ReviewStatus == ListingReviewStatus.Approved
                    && (mb.SaleState == SaleState.OnSale || mb.SaleState == SaleState.Restocking)))
            .Where(mb => mb.Merchant != null)
            .GroupBy(mb => mb.Merchant!.Id)
            .Select(g =>
            {
                // 同一商家多条关联取最优条目（在售优先），价格用该条目自己的报价
                var best = g.OrderByDescending(mb => mb.SaleState == SaleState.OnSale).First();
                // 改动说明（v2.3.0 商城置顶卡）：商家级置顶态取该商家所有关联里最晚的未过期置顶——
                // 置顶是"买曝光"，只要有一条在置顶期，该商家就排前面并带角标
                var pinnedUntil = g.Where(mb => mb.PinnedUntil.HasValue && mb.PinnedUntil.Value > now)
                    .Select(mb => mb.PinnedUntil!.Value)
                    .DefaultIfEmpty(DateTime.MinValue)
                    .Max();
                return new
                {
                    Dto = new BearingMerchantDto
                    {
                        MerchantId = g.Key,
                        MerchantName = best.Merchant!.Name,
                        Price = best.PriceDescription,
                        SaleState = best.SaleState,
                        RestockEta = best.RestockEta,
                        // 改动说明（v1.6.0 数据可信联动）：透出认证标记——行动门"询价只对认证商家开放"的判定依据
                        IsVerified = best.Merchant!.IsVerified,
                        IsPinned = pinnedUntil > DateTime.MinValue,
                        PinnedUntil = pinnedUntil > DateTime.MinValue ? pinnedUntil : null
                    },
                    // 改动说明（v2.15.0 搜索权重批次）：携带商家等级序数用于排序（与商家搜索同口径）
                    GradeRank = best.Merchant!.Grade.Rank()
                };
            })
            // 置顶商家优先；其次按商家等级序数（等级高者靠前）；LINQ OrderBy 为稳定排序，其余保持原有相对顺序
            .OrderByDescending(x => x.Dto.IsPinned)
            .ThenByDescending(x => x.GradeRank)
            .Select(x => x.Dto)
            .ToList();
        var total = merchants.Count;
        var paged = merchants
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new PagedResult<BearingMerchantDto>
        {
            Items = paged,
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}

/// <summary>
/// 轴承商家列表行 DTO（v1.36.0）：对齐 BFF BearingMerchantItem 契约——
/// 商家公开摘要 + 该商家对此轴承的报价与三态（在售/补货中徽标）
/// </summary>
public class BearingMerchantDto
{
    /// <summary>商家ID</summary>
    public Guid MerchantId { get; set; }

    /// <summary>商家名称</summary>
    public string MerchantName { get; set; } = string.Empty;

    /// <summary>该商家对此轴承的报价描述（可空）</summary>
    public string? Price { get; set; }

    /// <summary>销售状态（v2.15.0）：0=已下架 1=出售中 2=补货中（前端据此渲染在售/补货徽标）</summary>
    public SaleState SaleState { get; set; }

    /// <summary>补货预计到货时间（自由文本，可空）</summary>
    public string? RestockEta { get; set; }

    /// <summary>商家是否已认证（v1.6.0 数据可信联动；询价行动门只对认证商家开放）</summary>
    public bool IsVerified { get; set; }

    /// <summary>是否处于置顶期（v2.3.0 商城置顶卡；前端渲染"置顶"角标）</summary>
    public bool IsPinned { get; set; }

    /// <summary>置顶到期时间（UTC，可空；前端可做剩余时长提示）</summary>
    public DateTime? PinnedUntil { get; set; }
}
