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
                SortBy = request.SortBy,
                SortOrder = request.SortOrder,
                IsAuthenticated = request.IsAuthenticated,
                Page = request.Page,
                PageSize = request.PageSize
            };

            var result = await _merchantBearingRepository.SearchMallFeedAsync(searchParams, cancellationToken);
            _logger.LogInformation("商城货源流: Keyword={Keyword}, BrandId={BrandId}, Page={Page}, Total={Total}",
                request.Keyword, request.BrandId, request.Page, result.TotalCount);

            return new PagedResult<MerchantBearingDto>
            {
                Items = result.Items.Select(mb => mb.ToDto(request.IsAuthenticated)).ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }
    }
}