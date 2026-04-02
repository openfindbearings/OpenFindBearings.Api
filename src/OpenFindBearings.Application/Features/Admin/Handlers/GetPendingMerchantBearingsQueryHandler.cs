using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Admin.DTOs;
using OpenFindBearings.Application.Features.Admin.Queries;
using OpenFindBearings.Application.Features.Bearings.DTOs;
using OpenFindBearings.Application.Features.Merchants.DTOs;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Admin.Handlers
{
    /// <summary>
    /// 获取待审核商家产品查询处理器
    /// </summary>
    public class GetPendingMerchantBearingsQueryHandler : IRequestHandler<GetPendingMerchantBearingsQuery, PagedResult<PendingMerchantBearingDto>>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly ILogger<GetPendingMerchantBearingsQueryHandler> _logger;

        public GetPendingMerchantBearingsQueryHandler(
            IMerchantBearingRepository merchantBearingRepository,
            IMerchantRepository merchantRepository,
            IBearingRepository bearingRepository,
            ILogger<GetPendingMerchantBearingsQueryHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _merchantRepository = merchantRepository;
            _bearingRepository = bearingRepository;
            _logger = logger;
        }

        public async Task<PagedResult<PendingMerchantBearingDto>> Handle(
            GetPendingMerchantBearingsQuery request,
            CancellationToken cancellationToken)
        {
            // 获取待审核的商家产品关联
            var pendingItems = await _merchantBearingRepository.GetPendingApprovalAsync(cancellationToken);

            // 分页处理
            var items = pendingItems
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var result = new List<PendingMerchantBearingDto>();

            foreach (var item in items)
            {
                var merchant = await _merchantRepository.GetByIdAsync(item.MerchantId, cancellationToken);
                var bearing = await _bearingRepository.GetByIdAsync(item.BearingId, cancellationToken);

                result.Add(new PendingMerchantBearingDto
                {
                    Id = item.Id,
                    Merchant = merchant != null ? MapMerchantToDto(merchant) : null!,
                    Bearing = bearing != null ? MapBearingToDto(bearing) : null!,
                    PriceDescription = item.PriceDescription,
                    StockDescription = item.StockDescription,
                    MinOrderDescription = item.MinOrderDescription,
                    Remarks = item.Remarks,
                    CreatedAt = item.CreatedAt,
                    SubmitterName = "待实现" // TODO: 从用户仓储获取提交人
                });
            }

            return new PagedResult<PendingMerchantBearingDto>
            {
                Items = result,
                TotalCount = pendingItems.Count(),
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        private MerchantDto MapMerchantToDto(Merchant merchant)
        {
            return new MerchantDto
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
                ProductCount = merchant.MerchantBearings?.Count ?? 0
            };
        }

        private BearingDto MapBearingToDto(Bearing bearing)
        {
            return new BearingDto
            {
                Id = bearing.Id,
                PartNumber = bearing.PartNumber,
                Name = bearing.Name,
                Description = bearing.Description,
                InnerDiameter = bearing.Dimensions.InnerDiameter,
                OuterDiameter = bearing.Dimensions.OuterDiameter,
                Width = bearing.Dimensions.Width,
                Weight = bearing.Weight,
                BrandId = bearing.BrandId,
                BrandName = bearing.Brand?.Name ?? string.Empty,
                BearingTypeId = bearing.BearingTypeId,
                BearingTypeName = bearing.BearingType?.Name ?? string.Empty,
                ViewCount = bearing.ViewCount,
                FavoriteCount = bearing.FavoriteCount
            };
        }
    }
}
