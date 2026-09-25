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
                return new BearingMerchantDto
                {
                    MerchantId = g.Key,
                    MerchantName = best.Merchant!.Name,
                    Price = best.PriceDescription,
                    IsOnSale = best.IsOnSale,
                    IsRestocking = best.IsRestocking,
                    RestockEta = best.RestockEta
                };
            })
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
}
