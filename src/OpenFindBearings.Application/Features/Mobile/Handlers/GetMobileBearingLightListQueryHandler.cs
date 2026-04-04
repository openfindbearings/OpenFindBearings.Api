using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Mobile.DTOs;
using OpenFindBearings.Application.Features.Mobile.Queries;
using OpenFindBearings.Domain.Repositories;
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

            // ✅ SearchAsync 返回 PagedResult<Bearing>
            var result = await _bearingRepository.SearchAsync(searchParams, cancellationToken);

            var items = new List<MobileBearingLightDto>();
            // ✅ 遍历 result.Items，而不是 result 本身
            foreach (var bearing in result.Items)
            {
                var minPrice = await GetMinPriceAsync(bearing.Id, cancellationToken);

                items.Add(new MobileBearingLightDto
                {
                    Id = bearing.Id,
                    CurrentCode = bearing.CurrentCode,           // ✅ 修改
                    FormerCode = bearing.FormerCode,             // ✅ 新增
                    Name = bearing.Name,                         // ✅ 新增
                    BrandName = bearing.Brand?.Name ?? string.Empty,
                    BearingTypeName = bearing.BearingType,       // ✅ 新增
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
                TotalCount = result.TotalCount,    // ✅ 使用 result.TotalCount
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

            var prices = merchantBearings
                .Where(mb => mb.IsOnSale && mb.NumericPrice.HasValue)
                .Select(mb => mb.NumericPrice.GetValueOrDefault());

            return prices.Any() ? prices.Min() : null;
        }
    }

}
