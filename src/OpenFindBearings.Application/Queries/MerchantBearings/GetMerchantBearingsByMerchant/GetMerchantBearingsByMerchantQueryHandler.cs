using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.MerchantBearings.GetMerchantBearingsByMerchant
{
    /// <summary>
    /// 获取指定商家的所有关联查询处理器
    /// </summary>
    public class GetMerchantBearingsByMerchantQueryHandler : IRequestHandler<GetMerchantBearingsByMerchantQuery, PagedResult<MerchantBearingDto>>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly IPriceConfigProvider _priceConfigProvider;
        private readonly ILogger<GetMerchantBearingsByMerchantQueryHandler> _logger;

        public GetMerchantBearingsByMerchantQueryHandler(
            IMerchantBearingRepository merchantBearingRepository,
            IPriceConfigProvider priceConfigProvider,
            ILogger<GetMerchantBearingsByMerchantQueryHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _priceConfigProvider = priceConfigProvider;
            _logger = logger;
        }

        public async Task<PagedResult<MerchantBearingDto>> Handle(
            GetMerchantBearingsByMerchantQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取商家轴承列表: MerchantId={MerchantId}, IsAuthenticated={IsAuthenticated}",
                request.MerchantId, request.IsAuthenticated);

            var result = await _merchantBearingRepository.GetMerchantBearingsPagedAsync(
                request.MerchantId, request.OnlyOnSale, request.PendingOnly, request.DataSource,
                request.Page, request.PageSize, cancellationToken);

            // 排序：created / price / viewcount
            // 改动说明：按价格排序前检查 Price.NumericForSorting 配置开关，关闭时忽略 sortBy=price
            //           回退到默认的时间排序，使该配置项在服务端真正生效（不再依赖调用方自觉）
            IEnumerable<MerchantBearing> sorted = result.Items;
            var sortBy = request.SortBy?.ToLower();
            if (string.Equals(sortBy, "price", StringComparison.OrdinalIgnoreCase))
            {
                var priceConfig = await _priceConfigProvider.GetAsync(cancellationToken);
                if (!priceConfig.NumericForSorting)
                {
                    _logger.LogInformation("价格排序已关闭（Price.NumericForSorting=false），回退为时间排序");
                    sortBy = null;
                }
            }

            var asc = string.Equals(request.SortOrder, "asc", StringComparison.OrdinalIgnoreCase);
            sorted = sortBy switch
            {
                "price" => asc
                    ? sorted.OrderBy(mb => mb.NumericPrice)
                    : sorted.OrderByDescending(mb => mb.NumericPrice),
                "viewcount" => asc
                    ? sorted.OrderBy(mb => mb.ViewCount)
                    : sorted.OrderByDescending(mb => mb.ViewCount),
                _ => asc
                    ? sorted.OrderBy(mb => mb.CreatedAt)
                    : sorted.OrderByDescending(mb => mb.CreatedAt)
            };

            return new PagedResult<MerchantBearingDto>
            {
                Items = sorted.Select(mb => mb.ToDto(request.IsAuthenticated)).ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }
    }
}
