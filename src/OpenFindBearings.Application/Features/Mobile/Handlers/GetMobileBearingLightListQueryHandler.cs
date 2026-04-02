using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Mobile.DTOs;
using OpenFindBearings.Application.Features.Mobile.Queries;
using OpenFindBearings.Domain.Interfaces;
using OpenFindBearings.Domain.Specifications;

namespace OpenFindBearings.Application.Features.Mobile.Handlers
{
    /// <summary>
    /// 获取移动端轴承轻量列表查询处理器
    /// </summary>
    public class GetMobileBearingLightListQueryHandler : IRequestHandler<GetMobileBearingLightListQuery, PagedResult<MobileBearingLightDto>>
    {
        private readonly IBearingRepository _bearingRepository;
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly ILogger<GetMobileBearingLightListQueryHandler> _logger;

        public GetMobileBearingLightListQueryHandler(
            IBearingRepository bearingRepository,
            IMerchantBearingRepository merchantBearingRepository,
            ILogger<GetMobileBearingLightListQueryHandler> logger)
        {
            _bearingRepository = bearingRepository;
            _merchantBearingRepository = merchantBearingRepository;
            _logger = logger;
        }

        public async Task<PagedResult<MobileBearingLightDto>> Handle(
            GetMobileBearingLightListQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取移动端轴承轻量列表: Keyword={Keyword}, Page={Page}",
                request.Keyword, request.Page);

            var searchParams = new BearingSearchParams
            {
                Keyword = request.Keyword,
                MinInnerDiameter = request.MinInnerDiameter,
                MaxInnerDiameter = request.MaxInnerDiameter,
                BrandId = request.BrandId,
                Page = request.Page,
                PageSize = request.PageSize
            };

            // 获取当前页的数据
            var bearings = await _bearingRepository.SearchAsync(searchParams, cancellationToken);

            // 获取总记录数
            var totalCount = await _bearingRepository.GetTotalCountAsync(searchParams, cancellationToken);

            var items = new List<MobileBearingLightDto>();
            foreach (var bearing in bearings)
            {
                // 获取该轴承的最低价格
                var minPrice = await GetMinPriceAsync(bearing.Id, cancellationToken);

                items.Add(new MobileBearingLightDto
                {
                    Id = bearing.Id,
                    PartNumber = bearing.PartNumber,
                    BrandName = bearing.Brand?.Name ?? string.Empty,
                    InnerDiameter = bearing.Dimensions.InnerDiameter,
                    OuterDiameter = bearing.Dimensions.OuterDiameter,
                    Width = bearing.Dimensions.Width,
                    ThumbnailUrl = $"/images/bearings/{bearing.Id}.jpg",
                    MinPrice = minPrice,
                    OriginCountry = bearing.OriginCountry,
                    Category = bearing.Category.ToString()
                });
            }

            return new PagedResult<MobileBearingLightDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        /// <summary>
        /// 获取轴承的最低价格
        /// </summary>
        private async Task<decimal?> GetMinPriceAsync(Guid bearingId, CancellationToken cancellationToken)
        {
            var merchantBearings = await _merchantBearingRepository.GetByBearingAsync(bearingId, cancellationToken);

            // 只考虑在售且有数值化价格的商品
            var prices = merchantBearings
                .Where(mb => mb.IsOnSale && mb.NumericPrice.HasValue)
                .Select(mb => mb.NumericPrice.GetValueOrDefault());

            return prices.Any() ? prices.Min() : null;
        }
    }
}
