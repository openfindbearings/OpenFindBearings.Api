using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
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

        // 改动说明（v1.36.0 三态）：在售筛选口径从"仅 IsOnSale"扩为"在售+补货中"——
        // 补货中商家仍出现在列表（带徽标），仅已下架商家被排除；
        // 徽标按商家聚合：该商家对本轴承只要有一条在售关联就不算补货中
        var merchants = relations
            .Where(mb => !request.OnlyOnSale.HasValue || !request.OnlyOnSale.Value || mb.IsOnSale || mb.IsRestocking)
            .Where(mb => mb.Merchant != null)
            .GroupBy(mb => mb.Merchant!.Id)
            .Select(g =>
            {
                // 同一商家多条关联取最优条目（在售优先），价格用该条目自己的报价
                var best = g.OrderByDescending(mb => mb.IsOnSale).First();
                // 改动说明（v2.3.0 商城置顶卡）：商家级置顶态取该商家所有关联里最晚的未过期置顶——
                // 置顶是"买曝光"，只要有一条在置顶期，该商家就排前面并带角标
                var pinnedUntil = g.Where(mb => mb.PinnedUntil.HasValue && mb.PinnedUntil.Value > now)
                    .Select(mb => mb.PinnedUntil!.Value)
                    .DefaultIfEmpty(DateTime.MinValue)
                    .Max();
                return new BearingMerchantDto
                {
                    MerchantId = g.Key,
                    MerchantName = best.Merchant!.Name,
                    Price = best.PriceDescription,
                    IsOnSale = best.IsOnSale,
                    IsRestocking = best.IsRestocking,
                    RestockEta = best.RestockEta,
                    IsPinned = pinnedUntil > DateTime.MinValue,
                    PinnedUntil = pinnedUntil > DateTime.MinValue ? pinnedUntil : null
                };
            })
            // 置顶商家优先，其余保持原有相对顺序（稳定排序）
            .OrderByDescending(m => m.IsPinned)
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

    /// <summary>是否在售</summary>
    public bool IsOnSale { get; set; }

    /// <summary>是否补货中（有该型号但当前无货，仍展示带徽标）</summary>
    public bool IsRestocking { get; set; }

    /// <summary>补货预计到货时间（自由文本，可空）</summary>
    public string? RestockEta { get; set; }

    /// <summary>是否处于置顶期（v2.3.0 商城置顶卡；前端渲染"置顶"角标）</summary>
    public bool IsPinned { get; set; }

    /// <summary>置顶到期时间（UTC，可空；前端可做剩余时长提示）</summary>
    public DateTime? PinnedUntil { get; set; }
}
