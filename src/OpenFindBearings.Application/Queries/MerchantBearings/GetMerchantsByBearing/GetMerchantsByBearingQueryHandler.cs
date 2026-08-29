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
public class GetMerchantsByBearingQueryHandler : IRequestHandler<GetMerchantsByBearingQuery, PagedResult<MerchantDto>>
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

    public async Task<PagedResult<MerchantDto>> Handle(
        GetMerchantsByBearingQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("获取轴承在售商家: BearingId={BearingId}, OnlyOnSale={OnlyOnSale}",
            request.BearingId, request.OnlyOnSale);

        // 改动说明：按轴承反查所有商家轴承关联（含 Merchant 导航），再据此得到在售商家列表
        var relations = await _merchantBearingRepository.GetByBearingAsync(request.BearingId, cancellationToken);

        var merchants = relations
            .Where(mb => !request.OnlyOnSale.HasValue || !request.OnlyOnSale.Value || mb.IsOnSale)
            .Select(mb => mb.Merchant)
            .Where(m => m != null)
            // 同一轴承可能被同一商家多条关联引用，按商家ID去重
            .GroupBy(m => m!.Id)
            .Select(g => g.First())
            .ToList();

        var total = merchants.Count;
        var paged = merchants
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => m!.ToPublicDto())
            .ToList();

        return new PagedResult<MerchantDto>
        {
            Items = paged,
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
