using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Services;

namespace OpenFindBearings.Application.Commands.MerchantBearings.UpdateMerchantBearing
{
    /// <summary>
    /// 更新商家-轴承关联命令处理器
    /// </summary>
    public class UpdateMerchantBearingCommandHandler : IRequestHandler<UpdateMerchantBearingCommand>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly IPriceConfigProvider _priceConfigProvider;
        private readonly ILogger<UpdateMerchantBearingCommandHandler> _logger;

        public UpdateMerchantBearingCommandHandler(
            IMerchantBearingRepository merchantBearingRepository,
            IMerchantMemberRepository merchantMemberRepository,
            IPriceConfigProvider priceConfigProvider,
            ILogger<UpdateMerchantBearingCommandHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _merchantMemberRepository = merchantMemberRepository;
            _priceConfigProvider = priceConfigProvider;
            _logger = logger;
        }

        public async Task Handle(
            UpdateMerchantBearingCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始更新商家-轴承关联: Id={Id}", request.Id);

            var merchantBearing = await _merchantBearingRepository.GetByIdAsync(request.Id, cancellationToken);
            if (merchantBearing == null)
            {
                throw new InvalidOperationException($"商家-轴承关联不存在: {request.Id}");
            }

            // 所有权验证：当前用户必须是该商户的在职成员（成员表判定，支持一人多商户）
            var member = await _merchantMemberRepository.GetActiveByUserAndMerchantAsync(
                request.UserId, merchantBearing.MerchantId, cancellationToken);
            if (member == null)
            {
                throw new UnauthorizedAccessException("无权修改其他商家的轴承信息");
            }

            // 检查是否有变化
            bool hasChanges = false;

            if (request.PriceDescription != merchantBearing.PriceDescription ||
                request.StockDescription != merchantBearing.StockDescription ||
                request.MinOrderDescription != merchantBearing.MinOrderDescription ||
                request.Remarks != merchantBearing.Remarks)
            {
                hasChanges = true;
            }

            // 更新市场信息
            merchantBearing.UpdateMarketInfo(
                request.PriceDescription,
                request.StockDescription,
                request.MinOrderDescription,
                request.Remarks);

            // 改动说明（v2.23.0 批量改价排查时发现）：原实现只改价格文本、不重算 NumericPrice，
            //   改价后价格排序与筛选用的一直是旧值；这里与批量调价保持同一口径重新解析数值价，
            //   价格可见性沿用商家原有设置
            var priceConfig = await _priceConfigProvider.GetAsync(cancellationToken);
            merchantBearing.SetPrice(
                request.PriceDescription,
                PriceParser.ExtractNumericPrice(request.PriceDescription, priceConfig.ExtractPattern),
                merchantBearing.PriceVisibility);

            // v2.15.0 状态统一：仅"已通过"的商品改动重要字段才重审（重新变审核中、公域暂隐）；
            //   仓库中/已驳回保持原态（本就未公开，无需重审）
            if (hasChanges && merchantBearing.ReviewStatus == ListingReviewStatus.Approved)
            {
                merchantBearing.SubmitForApproval();
            }

            await _merchantBearingRepository.UpdateAsync(merchantBearing, cancellationToken);

            _logger.LogInformation("商家-轴承关联更新成功: Id={Id}", merchantBearing.Id);
        }
    }
}
