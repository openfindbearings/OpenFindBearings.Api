using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.MerchantBearings.DTOs;
using OpenFindBearings.Application.Features.MerchantBearings.Queries;
using OpenFindBearings.Domain.Common.Models;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.MerchantBearings.Handlers
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
            _logger.LogInformation("获取商家轴承列表: MerchantId={MerchantId}, Page={Page}, PageSize={PageSize}",
                request.MerchantId, request.Page, request.PageSize);

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
