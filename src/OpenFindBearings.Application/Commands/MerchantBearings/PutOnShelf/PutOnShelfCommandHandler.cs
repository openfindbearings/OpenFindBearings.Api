using MediatR;
using OpenFindBearings.Application.Services;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Application.Services;

namespace OpenFindBearings.Application.Commands.MerchantBearings.PutOnShelf
{
    /// <summary>
    /// 上架产品命令处理器
    /// </summary>
    public class PutOnShelfCommandHandler : IRequestHandler<PutOnShelfCommand>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly IMerchantRepository _merchantRepository;
        // v1.34.0：首件商品上架一次性积分（bizId 绑信用代码，删店重入驻不重复发）
        private readonly IPointsService _pointsService;
        private readonly ILogger<PutOnShelfCommandHandler> _logger;

        public PutOnShelfCommandHandler(
            IMerchantBearingRepository merchantBearingRepository,
            IMerchantMemberRepository merchantMemberRepository,
            IMerchantRepository merchantRepository,
            IPointsService pointsService,
            ILogger<PutOnShelfCommandHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _merchantMemberRepository = merchantMemberRepository;
            _merchantRepository = merchantRepository;
            _pointsService = pointsService;
            _logger = logger;
        }

        public async Task Handle(
            PutOnShelfCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始上架产品: MerchantBearingId={MerchantBearingId}", request.MerchantBearingId);

            var merchantBearing = await _merchantBearingRepository.GetByIdAsync(request.MerchantBearingId, cancellationToken);
            if (merchantBearing == null)
            {
                throw new InvalidOperationException($"商家-轴承关联不存在: {request.MerchantBearingId}");
            }

            // 所有权验证：当前用户必须是该商户的在职成员（成员表判定，支持一人多商户）
            var member = await _merchantMemberRepository.GetActiveByUserAndMerchantAsync(
                request.UserId, merchantBearing.MerchantId, cancellationToken);
            if (member == null)
            {
                throw new UnauthorizedAccessException("无权修改其他商家的轴承信息");
            }

            merchantBearing.PutOnShelf();
            await _merchantBearingRepository.UpdateAsync(merchantBearing, cancellationToken);

            // 改动说明（v1.34.0 积分防刷设计）：首件上架一次性奖励——bizId 绑信用代码，
            //   该商户历史上首次发分后，后续每次上架都被幂等跳过（无需计数查询）；
            //   无信用代码的商户跳过发放（无法唯一定位，补码后首次上架自动生效）；
            //   GrantAsync 吞异常，积分失败不影响上架主流程
            var merchant = await _merchantRepository.GetByIdAsync(merchantBearing.MerchantId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(merchant?.UnifiedSocialCreditCode))
            {
                 await _pointsService.GrantOneTimeAsync(
                     request.UserId,
                     PointTransaction.TypeMerchantFirstProduct,
            $"credit:{merchant.UnifiedSocialCreditCode.Trim().ToUpperInvariant()}:product",
            $"商户「{merchant.Name}」首件商品上架",
                     cancellationToken);
            }

            _logger.LogInformation("产品上架成功: MerchantBearingId={MerchantBearingId}", merchantBearing.Id);
        }
    }
}
