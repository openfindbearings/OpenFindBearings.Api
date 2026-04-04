using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.MerchantBearings.DTOs;
using OpenFindBearings.Application.Features.Merchants.DTOs;
using OpenFindBearings.Application.Features.Merchants.Queries;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Merchants.Handlers
{
    /// <summary>
    /// 获取商家查询处理器
    /// </summary>
    public class GetMerchantQueryHandler : IRequestHandler<GetMerchantQuery, MerchantDetailDto?>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly ILogger<GetMerchantQueryHandler> _logger;

        public GetMerchantQueryHandler(
            IMerchantRepository merchantRepository,
            IMerchantBearingRepository merchantBearingRepository,
            ILogger<GetMerchantQueryHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _merchantBearingRepository = merchantBearingRepository;
            _logger = logger;
        }

        public async Task<MerchantDetailDto?> Handle(GetMerchantQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取商家详情: MerchantId={MerchantId}, IsAuthenticated={IsAuthenticated}",
                request.Id, request.IsAuthenticated);

            var merchant = await _merchantRepository.GetByIdAsync(request.Id, cancellationToken);
            if (merchant == null)
                return null;

            // 获取商家在售产品列表
            var merchantBearings = await _merchantBearingRepository.GetOnSaleByMerchantAsync(request.Id, cancellationToken);

            var products = merchantBearings.Select(mb => new MerchantBearingDto
            {
                Id = mb.Id,
                MerchantId = mb.MerchantId,

                // ========== 敏感信息控制 ==========
                MerchantName = request.IsAuthenticated ? merchant.Name : null,
                MerchantGrade = merchant.Grade.ToString(),
                MerchantIsVerified = merchant.IsVerified,
                MerchantCity = ExtractCityFromAddress(merchant.Contact?.Address),
                MerchantPhone = request.IsAuthenticated ? merchant.Contact?.Phone : null,
                MerchantAddress = request.IsAuthenticated ? merchant.Contact?.Address : null,

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
            }).ToList();

            return MapToDetailDto(merchant, products, request.IsAuthenticated);
        }

        private MerchantDetailDto MapToDetailDto(Merchant merchant, List<MerchantBearingDto> products, bool isAuthenticated)
        {
            return new MerchantDetailDto
            {
                Id = merchant.Id,
                Name = merchant.Name,
                CompanyName = merchant.CompanyName,
                Type = merchant.Type.ToString(),
                // ========== 敏感信息控制 ==========
                ContactPerson = isAuthenticated ? merchant.Contact?.ContactPerson : null,
                Phone = isAuthenticated ? merchant.Contact?.Phone : null,
                Mobile = isAuthenticated ? merchant.Contact?.Mobile : null,
                Email = isAuthenticated ? merchant.Contact?.Email : null,
                Address = isAuthenticated ? merchant.Contact?.Address : null,
                IsVerified = merchant.IsVerified,
                Grade = merchant.Grade.ToString(),
                FollowerCount = merchant.FollowerCount,
                ProductCount = products.Count,
                Description = merchant.Description,
                BusinessScope = merchant.BusinessScope,
                VerifiedAt = merchant.VerifiedAt,
                Staff = merchant.Staff?.Select(s => new MerchantStaffDto
                {
                    Id = s.Id,
                    Nickname = s.Nickname ?? string.Empty,
                    Avatar = s.Avatar,
                    Role = "员工"
                }).ToList() ?? [],
                Products = products
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
