using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.MerchantBearings.DTOs;
using OpenFindBearings.Application.Features.Merchants.DTOs;
using OpenFindBearings.Application.Features.Merchants.Queries;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Interfaces;

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
            _logger.LogInformation("获取商家详情: MerchantId={MerchantId}", request.Id);

            var merchant = await _merchantRepository.GetByIdAsync(request.Id, cancellationToken);
            if (merchant == null)
                return null;

            // 获取商家在售产品列表
            var merchantBearings = await _merchantBearingRepository.GetOnSaleByMerchantAsync(request.Id, cancellationToken);

            var products = merchantBearings.Select(mb => new MerchantBearingDto
            {
                Id = mb.Id,
                MerchantId = mb.MerchantId,
                MerchantName = merchant.Name,
                BearingId = mb.BearingId,
                BearingPartNumber = mb.Bearing?.PartNumber ?? string.Empty,
                BearingName = mb.Bearing?.Name ?? string.Empty,
                BrandName = mb.Bearing?.Brand?.Name,
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
                UpdatedAt = mb.UpdatedAt
                // IsPriceVisible 由 API 层根据登录状态设置
            }).ToList();

            return MapToDetailDto(merchant, products);
        }

        private MerchantDetailDto MapToDetailDto(Merchant merchant, List<MerchantBearingDto> products)
        {
            return new MerchantDetailDto
            {
                Id = merchant.Id,
                Name = merchant.Name,
                CompanyName = merchant.CompanyName,
                Type = merchant.Type.ToString(),
                ContactPerson = merchant.Contact?.ContactPerson,
                Phone = merchant.Contact?.Phone,
                Mobile = merchant.Contact?.Mobile,
                Email = merchant.Contact?.Email,
                Address = merchant.Contact?.Address,
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
                    Role = "员工"  // 可根据实际角色设置
                }).ToList() ?? [],
                Products = products
            };
        }
    }
}
