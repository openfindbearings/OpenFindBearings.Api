using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.MerchantBearings.GetMerchantBearingsByMerchant
{
    /// <summary>
    /// 获取指定商家的所有关联查询处理器
    /// </summary>
    public class GetMerchantBearingsByMerchantQueryHandler : IRequestHandler<GetMerchantBearingsByMerchantQuery, PagedResult<MerchantBearingDto>>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly ILogger<GetMerchantBearingsByMerchantQueryHandler> _logger;

        public GetMerchantBearingsByMerchantQueryHandler(
            IMerchantBearingRepository merchantBearingRepository,
            ILogger<GetMerchantBearingsByMerchantQueryHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _logger = logger;
        }

        public async Task<PagedResult<MerchantBearingDto>> Handle(
            GetMerchantBearingsByMerchantQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取商家轴承列表: MerchantId={MerchantId}, IsAuthenticated={IsAuthenticated}",
                request.MerchantId, request.IsAuthenticated);

            var merchantBearings = await _merchantBearingRepository.GetByMerchantAsync(request.MerchantId, cancellationToken);

            if (request.OnlyOnSale.HasValue)
            {
                merchantBearings = merchantBearings.Where(mb => mb.IsOnSale == request.OnlyOnSale.Value);
            }

            var totalCount = merchantBearings.Count();
            var items = merchantBearings
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(mb => new MerchantBearingDto
                {
                    Id = mb.Id,
                    MerchantId = mb.MerchantId,

                    // ========== 敏感信息控制 ==========
                    // 规则1：商家名称 - 仅登录用户可见
                    MerchantName = request.IsAuthenticated ? mb.Merchant?.Name : null,

                    // 规则2：商家等级 - 所有人可见
                    MerchantGrade = mb.Merchant?.Grade.ToString() ?? string.Empty,

                    // 规则3：商家认证状态 - 所有人可见
                    MerchantIsVerified = mb.Merchant?.IsVerified ?? false,

                    // 规则4：商家所在城市 - 所有人可见（从地址提取）
                    MerchantCity = ExtractCityFromAddress(mb.Merchant?.Contact?.Address),

                    // 规则5：商家电话 - 仅登录用户可见
                    MerchantPhone = request.IsAuthenticated ? mb.Merchant?.Contact?.Phone : null,

                    // 规则6：商家详细地址 - 仅登录用户可见
                    MerchantAddress = request.IsAuthenticated ? mb.Merchant?.Contact?.Address : null,

                    // ========== 轴承信息 ==========
                    BearingId = mb.BearingId,
                    BearingCurrentCode = mb.Bearing?.CurrentCode ?? string.Empty,
                    BearingFormerCode = mb.Bearing?.FormerCode,
                    BearingName = mb.Bearing?.Name ?? string.Empty,
                    BearingTypeName = mb.Bearing?.BearingType,

                    // ========== 品牌信息 ==========
                    BrandName = mb.Bearing?.Brand?.Name,
                    BrandLevel = mb.Bearing?.Brand?.Level.ToString(),

                    // ========== 尺寸信息 ==========
                    Dimensions = mb.Bearing != null
                        ? $"{mb.Bearing.Dimensions.InnerDiameter}×{mb.Bearing.Dimensions.OuterDiameter}×{mb.Bearing.Dimensions.Width}"
                        : null,

                    // ========== 价格信息 ==========
                    PriceDescription = mb.PriceDescription,
                    PriceVisibility = mb.PriceVisibility,
                    NumericPrice = mb.NumericPrice,
                    IsPriceVisible = mb.IsPriceVisible(request.IsAuthenticated),

                    // ========== 其他业务信息 ==========
                    StockDescription = mb.StockDescription,
                    MinOrderDescription = mb.MinOrderDescription,
                    Remarks = mb.Remarks,
                    IsOnSale = mb.IsOnSale,
                    IsFeatured = mb.IsFeatured,
                    IsPendingApproval = mb.IsPendingApproval,
                    ViewCount = mb.ViewCount,
                    CreatedAt = mb.CreatedAt,
                    UpdatedAt = mb.UpdatedAt
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

            // 简单实现：取地址中的城市部分
            // 实际项目中可以用正则或地址解析库
            var parts = fullAddress.Split(new[] { '省', '市', '区', '县' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 2)
            {
                // 返回第二个部分作为城市
                var city = parts[1].Trim();
                if (city.Length > 10) city = city.Substring(0, 10);
                return city;
            }

            // 如果无法解析，返回地址前6个字符
            return fullAddress.Length > 6 ? fullAddress.Substring(0, 6) : fullAddress;
        }
    }
}
