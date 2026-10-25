using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Specifications;

namespace OpenFindBearings.Application.Queries.MerchantBearings.GetMallFeed
{
    /// <summary>
    /// 商城货源流查询处理器（v2.14.0 商城导购）：组装过滤参数交仓储做 DB 级分页，
    /// 投影为 MerchantBearingDto（含商品卡所需的主图/品牌/尺寸/价格/商家）。
    /// </summary>
    public class GetMallFeedQueryHandler : IRequestHandler<GetMallFeedQuery, PagedResult<MerchantBearingDto>>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly ILogger<GetMallFeedQueryHandler> _logger;

        public GetMallFeedQueryHandler(
            IMerchantBearingRepository merchantBearingRepository,
            ILogger<GetMallFeedQueryHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _logger = logger;
        }

        /// <summary>执行货源流查询并投影为商品卡 DTO</summary>
        public async Task<PagedResult<MerchantBearingDto>> Handle(
            GetMallFeedQuery request, CancellationToken cancellationToken)
        {
            var searchParams = new MallFeedSearchParams
            {
                Keyword = request.Keyword,
                BrandId = request.BrandId,
                BearingTypeId = request.BearingTypeId,
                Category = request.Category,
                MinInnerDiameter = request.MinInnerDiameter,
                MaxInnerDiameter = request.MaxInnerDiameter,
                MinOuterDiameter = request.MinOuterDiameter,
                MaxOuterDiameter = request.MaxOuterDiameter,
                MinWidth = request.MinWidth,
                MaxWidth = request.MaxWidth,
                IsOnSale = true,
                // 改动说明（v2.21.1）：货源流只出认证商家货源；客户端未传时按 true 兜底
                VerifiedOnly = request.VerifiedOnly ?? true,
                SortBy = request.SortBy,
                SortOrder = request.SortOrder,
                // 改动说明：IsAuthenticated 改为可空（避免被 [AsParameters] 判为必填），未赋值时按匿名处理
                IsAuthenticated = request.IsAuthenticated ?? false,
                // 改动说明：分页改可空后在此兜底第 1 页、20 条（与声明为非空 int 时的默认值一致）
                Page = request.Page ?? 1,
                PageSize = request.PageSize ?? 20
            };

            var result = await _merchantBearingRepository.SearchMallFeedAsync(searchParams, cancellationToken);
            _logger.LogInformation("商城货源流: Keyword={Keyword}, BrandId={BrandId}, Page={Page}, Total={Total}",
                request.Keyword, request.BrandId, request.Page ?? 1, result.TotalCount);

            return new PagedResult<MerchantBearingDto>
            {
                Items = result.Items.Select(mb => mb.ToDto()).ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }
    }
}