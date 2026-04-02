using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.MerchantBearings.DTOs;
using OpenFindBearings.Application.Features.MerchantBearings.Queries;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.MerchantBearings.Handlers
{
    /// <summary>
    /// 搜索商家-轴承关联查询处理器
    /// </summary>
    public class SearchMerchantBearingsQueryHandler : IRequestHandler<SearchMerchantBearingsQuery, PagedResult<MerchantBearingDto>>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<SearchMerchantBearingsQueryHandler> _logger;

        public SearchMerchantBearingsQueryHandler(
            IMerchantBearingRepository merchantBearingRepository,
            IBearingRepository bearingRepository,
            IMerchantRepository merchantRepository,
            ILogger<SearchMerchantBearingsQueryHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _bearingRepository = bearingRepository;
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        public async Task<PagedResult<MerchantBearingDto>> Handle(
            SearchMerchantBearingsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("搜索商家-轴承关联: Keyword={Keyword}, Page={Page}, PageSize={PageSize}",
                request.Keyword, request.Page, request.PageSize);

            // 获取所有关联
            IEnumerable<MerchantBearing> merchantBearings = new List<MerchantBearing>();

            if (request.MerchantId.HasValue)
            {
                merchantBearings = await _merchantBearingRepository.GetByMerchantAsync(request.MerchantId.Value, cancellationToken);
            }
            else if (request.BearingId.HasValue)
            {
                merchantBearings = await _merchantBearingRepository.GetByBearingAsync(request.BearingId.Value, cancellationToken);
            }
            else
            {
                // 如果没有指定商家或轴承，返回空列表
                // 实际项目中可能需要更复杂的搜索逻辑
                return new PagedResult<MerchantBearingDto>
                {
                    Items = new List<MerchantBearingDto>(),
                    TotalCount = 0,
                    Page = request.Page,
                    PageSize = request.PageSize
                };
            }

            // 应用筛选条件
            if (request.IsOnSale.HasValue)
            {
                merchantBearings = merchantBearings.Where(mb => mb.IsOnSale == request.IsOnSale.Value);
            }

            if (request.IsFeatured.HasValue)
            {
                merchantBearings = merchantBearings.Where(mb => mb.IsFeatured == request.IsFeatured.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                merchantBearings = merchantBearings.Where(mb =>
                    (mb.Merchant?.Name?.Contains(request.Keyword, StringComparison.OrdinalIgnoreCase) == true) ||
                    (mb.Bearing?.PartNumber?.Contains(request.Keyword, StringComparison.OrdinalIgnoreCase) == true) ||
                    (mb.Bearing?.Name?.Contains(request.Keyword, StringComparison.OrdinalIgnoreCase) == true));
            }

            // 排序
            merchantBearings = request.SortBy?.ToLower() switch
            {
                "price" => request.SortOrder?.ToLower() == "desc"
                    ? merchantBearings.OrderByDescending(mb => mb.NumericPrice)
                    : merchantBearings.OrderBy(mb => mb.NumericPrice),
                "created" => request.SortOrder?.ToLower() == "desc"
                    ? merchantBearings.OrderByDescending(mb => mb.CreatedAt)
                    : merchantBearings.OrderBy(mb => mb.CreatedAt),
                "viewcount" => request.SortOrder?.ToLower() == "desc"
                    ? merchantBearings.OrderByDescending(mb => mb.ViewCount)
                    : merchantBearings.OrderBy(mb => mb.ViewCount),
                _ => merchantBearings.OrderByDescending(mb => mb.CreatedAt)
            };

            var totalCount = merchantBearings.Count();
            var items = merchantBearings
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(mb => new MerchantBearingDto
                {
                    Id = mb.Id,
                    MerchantId = mb.MerchantId,
                    MerchantName = mb.Merchant?.Name ?? string.Empty,
                    MerchantGrade = mb.Merchant?.Grade.ToString() ?? string.Empty,
                    MerchantIsVerified = mb.Merchant?.IsVerified ?? false,
                    BearingId = mb.BearingId,
                    BearingPartNumber = mb.Bearing?.PartNumber ?? string.Empty,
                    BearingName = mb.Bearing?.Name ?? string.Empty,
                    BrandName = mb.Bearing?.Brand?.Name,
                    BrandLevel = mb.Bearing?.Brand?.Level.ToString(),
                    Dimensions = mb.Bearing != null
                        ? $"{mb.Bearing.Dimensions.InnerDiameter}×{mb.Bearing.Dimensions.OuterDiameter}×{mb.Bearing.Dimensions.Width}"
                        : null,
                    PriceDescription = mb.PriceDescription,
                    PriceVisibility = mb.PriceVisibility,
                    NumericPrice = mb.NumericPrice,
                    StockDescription = mb.StockDescription,
                    MinOrderDescription = mb.MinOrderDescription,
                    Remarks = mb.Remarks,
                    IsOnSale = mb.IsOnSale,
                    IsFeatured = mb.IsFeatured,
                    IsPendingApproval = mb.IsPendingApproval,
                    ViewCount = mb.ViewCount,
                    CreatedAt = mb.CreatedAt,
                    UpdatedAt = mb.UpdatedAt,

                    // 使用传入的登录状态计算价格可见性
                    IsPriceVisible = mb.IsPriceVisible(request.IsAuthenticated)
                })
                .ToList();

            return new PagedResult<MerchantBearingDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
