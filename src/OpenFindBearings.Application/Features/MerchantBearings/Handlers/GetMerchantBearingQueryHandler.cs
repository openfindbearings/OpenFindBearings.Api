using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.MerchantBearings.DTOs;
using OpenFindBearings.Application.Features.MerchantBearings.Queries;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.MerchantBearings.Handlers
{
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

            var bearing = merchantBearing.Bearing;
            var merchant = merchantBearing.Merchant;

            return new MerchantBearingDto
            {
                Id = merchantBearing.Id,

                // 商家信息
                MerchantId = merchantBearing.MerchantId,
                MerchantName = merchant?.Name ?? string.Empty,
                MerchantGrade = merchant?.Grade.ToString() ?? string.Empty,
                MerchantIsVerified = merchant?.IsVerified ?? false,

                // 轴承信息
                BearingId = merchantBearing.BearingId,
                BearingCurrentCode = bearing?.CurrentCode ?? string.Empty,     // ✅ 修改
                BearingFormerCode = bearing?.FormerCode,                       // ✅ 新增
                BearingName = bearing?.Name ?? string.Empty,
                BearingTypeName = bearing?.BearingType,                        // ✅ 新增

                // 品牌信息
                BrandName = bearing?.Brand?.Name,
                BrandLevel = bearing?.Brand?.Level.ToString(),

                // 尺寸信息
                Dimensions = bearing != null
                    ? $"{bearing.Dimensions.InnerDiameter}×{bearing.Dimensions.OuterDiameter}×{bearing.Dimensions.Width}"
                    : null,

                // 价格信息
                PriceDescription = merchantBearing.PriceDescription,
                PriceVisibility = merchantBearing.PriceVisibility,
                NumericPrice = merchantBearing.NumericPrice,

                // 库存与起订量
                StockDescription = merchantBearing.StockDescription,
                MinOrderDescription = merchantBearing.MinOrderDescription,
                Remarks = merchantBearing.Remarks,

                // 状态
                IsOnSale = merchantBearing.IsOnSale,
                IsFeatured = merchantBearing.IsFeatured,
                IsPendingApproval = merchantBearing.IsPendingApproval,

                // 统计
                ViewCount = merchantBearing.ViewCount,
                CreatedAt = merchantBearing.CreatedAt,
                UpdatedAt = merchantBearing.UpdatedAt,

                // 价格可见性
                IsPriceVisible = merchantBearing.IsPriceVisible(request.IsAuthenticated)
            };
        }
    }
}
