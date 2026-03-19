using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.MerchantBearings.DTOs;
using OpenFindBearings.Application.Features.MerchantBearings.Queries;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.MerchantBearings.Handlers
{
    /// <summary>
    /// 获取单个商家-轴承关联查询处理器
    /// </summary>
    public class GetMerchantBearingQueryHandler : IRequestHandler<GetMerchantBearingQuery, MerchantBearingDto?>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly ILogger<GetMerchantBearingQueryHandler> _logger;

        public GetMerchantBearingQueryHandler(
            IMerchantBearingRepository merchantBearingRepository,
            ILogger<GetMerchantBearingQueryHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _logger = logger;
        }

        public async Task<MerchantBearingDto?> Handle(
            GetMerchantBearingQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取商家-轴承关联详情: Id={Id}", request.Id);

            var merchantBearing = await _merchantBearingRepository.GetByIdAsync(request.Id, cancellationToken);

            if (merchantBearing == null)
            {
                _logger.LogWarning("商家-轴承关联不存在: Id={Id}", request.Id);
                return null;
            }

            return new MerchantBearingDto
            {
                Id = merchantBearing.Id,
                MerchantId = merchantBearing.MerchantId,
                MerchantName = merchantBearing.Merchant?.Name ?? string.Empty,
                MerchantGrade = merchantBearing.Merchant?.Grade.ToString() ?? string.Empty,
                MerchantIsVerified = merchantBearing.Merchant?.IsVerified ?? false,
                BearingId = merchantBearing.BearingId,
                BearingPartNumber = merchantBearing.Bearing?.PartNumber ?? string.Empty,
                BearingName = merchantBearing.Bearing?.Name ?? string.Empty,
                BrandName = merchantBearing.Bearing?.Brand?.Name,
                BrandLevel = merchantBearing.Bearing?.Brand?.Level.ToString(),
                Dimensions = merchantBearing.Bearing != null
                    ? $"{merchantBearing.Bearing.Dimensions.InnerDiameter}×{merchantBearing.Bearing.Dimensions.OuterDiameter}×{merchantBearing.Bearing.Dimensions.Width}"
                    : null,
                PriceDescription = merchantBearing.PriceDescription,
                PriceVisibility = merchantBearing.PriceVisibility,
                NumericPrice = merchantBearing.NumericPrice,
                StockDescription = merchantBearing.StockDescription,
                MinOrderDescription = merchantBearing.MinOrderDescription,
                Remarks = merchantBearing.Remarks,
                IsOnSale = merchantBearing.IsOnSale,
                IsFeatured = merchantBearing.IsFeatured,
                IsPendingApproval = merchantBearing.IsPendingApproval,
                ViewCount = merchantBearing.ViewCount,
                CreatedAt = merchantBearing.CreatedAt,
                UpdatedAt = merchantBearing.UpdatedAt,

                // 使用传入的登录状态计算价格可见性
                IsPriceVisible = merchantBearing.IsPriceVisible(request.IsAuthenticated)
            };
        }
    }
}
