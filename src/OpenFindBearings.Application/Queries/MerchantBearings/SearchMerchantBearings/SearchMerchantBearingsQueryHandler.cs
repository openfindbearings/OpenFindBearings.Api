using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.MerchantBearings.SearchMerchantBearings
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
            _logger.LogInformation("搜索商家-轴承关联: Keyword={Keyword}, IsAuthenticated={IsAuthenticated}",
                request.Keyword, request.IsAuthenticated);

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
                    (mb.Bearing?.CurrentCode?.Contains(request.Keyword, StringComparison.OrdinalIgnoreCase) == true) ||
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

                    // ========== 敏感信息控制 ==========
                    MerchantName = request.IsAuthenticated ? mb.Merchant?.Name : null,
                    MerchantGrade = mb.Merchant?.Grade.ToString() ?? string.Empty,
                    MerchantIsVerified = mb.Merchant?.IsVerified ?? false,
                    MerchantCity = ExtractCityFromAddress(mb.Merchant?.Contact?.Address),
                    MerchantPhone = request.IsAuthenticated ? mb.Merchant?.Contact?.Phone : null,
                    MerchantAddress = request.IsAuthenticated ? mb.Merchant?.Contact?.Address : null,

                    BearingId = mb.BearingId,
                    BearingCurrentCode = mb.Bearing?.CurrentCode ?? string.Empty,
                    BearingFormerCode = mb.Bearing?.FormerCode,
                    BearingName = mb.Bearing?.Name ?? string.Empty,
                    BearingTypeName = mb.Bearing?.BearingType,
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

        /// <summary>
        /// 从完整地址中提取城市
        /// </summary>
        private string? ExtractCityFromAddress(string? fullAddress)
        {
            if (string.IsNullOrWhiteSpace(fullAddress))
                return null;

            var parts = fullAddress.Split(new[] { '省', '市', '区', '县' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 2)
            {
                var city = parts[1].Trim();
                if (city.Length > 10) city = city.Substring(0, 10);
                return city;
            }

            return fullAddress.Length > 6 ? fullAddress.Substring(0, 6) : fullAddress;
        }
    }
}
