using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.EventHandlers
{
    /// <summary>
    /// 纠错采纳成就处理器（v2.1.0 成就子系统）：订阅纠错审批事件，仅采纳分支给提交人
    /// 累加个人"纠错被采纳"计数。成就失败绝不影响主流程（try/catch 吞掉记日志）
    /// </summary>
    public class AchievementCorrectionHandler : INotificationHandler<CorrectionProcessedEvent>
    {
        private readonly IAchievementService _achievements;
        private readonly ILogger<AchievementCorrectionHandler> _logger;

        public AchievementCorrectionHandler(IAchievementService achievements, ILogger<AchievementCorrectionHandler> logger)
        {
            _achievements = achievements;
            _logger = logger;
        }

        /// <summary>采纳则累加个人纠错计数（点亮火眼金睛系列）</summary>
        public async Task Handle(CorrectionProcessedEvent notification, CancellationToken cancellationToken)
        {
            if (!notification.Approved)
                return;
            try
            {
                await _achievements.IncrementAsync(AchievementScope.Personal, notification.SubmittedBy,
                    "correction_adopted", 1, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "纠错成就累加失败（不影响主流程）: CorrectionId={Id}", notification.CorrectionId);
            }
        }
    }

    /// <summary>
    /// 寻货选定成就处理器（v2.1.0 成就子系统）：需求被选定关闭时，给应答商户累加商户轨
    /// "被选定"计数、给应答操作人累加个人轨"伯乐"计数
    /// </summary>
    public class AchievementSourcingClosedHandler : INotificationHandler<SourcingDemandClosedEvent>
    {
        private readonly IAchievementService _achievements;
        private readonly ISourcingResponseRepository _responses;
        private readonly ILogger<AchievementSourcingClosedHandler> _logger;

        public AchievementSourcingClosedHandler(IAchievementService achievements,
            ISourcingResponseRepository responses, ILogger<AchievementSourcingClosedHandler> logger)
        {
            _achievements = achievements;
            _responses = responses;
            _logger = logger;
        }

        /// <summary>商户轨+个人轨被选定计数累加</summary>
        public async Task Handle(SourcingDemandClosedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                await _achievements.IncrementAsync(AchievementScope.Merchant, notification.SelectedMerchantId,
                    "merchant_selected", 1, cancellationToken);

                var response = await _responses.GetByIdAsync(notification.SelectedResponseId, cancellationToken);
                if (response != null)
                {
                    await _achievements.IncrementAsync(AchievementScope.Personal, response.RespondedUserId,
                        "sourcing_selected", 1, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "寻货选定成就累加失败（不影响主流程）: DemandId={Id}", notification.DemandId);
            }
        }
    }

    /// <summary>
    /// 商品上架成就处理器（v2.1.0 成就子系统）：商品上架事件后重算该商户在售数并设仪表，
    /// 驱动"开张大吉/货架满满"商户轨成就
    /// </summary>
    public class AchievementListingHandler : INotificationHandler<BearingPutOnShelfEvent>
    {
        private readonly IAchievementService _achievements;
        private readonly IMerchantBearingRepository _bearings;
        private readonly ILogger<AchievementListingHandler> _logger;

        public AchievementListingHandler(IAchievementService achievements,
            IMerchantBearingRepository bearings, ILogger<AchievementListingHandler> logger)
        {
            _achievements = achievements;
            _bearings = bearings;
            _logger = logger;
        }

        /// <summary>重算在售数设仪表（取较大值防回退）</summary>
        public async Task Handle(BearingPutOnShelfEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                var onSale = await _bearings.GetOnSaleByMerchantAsync(notification.MerchantId, cancellationToken);
                await _achievements.SetGaugeAsync(AchievementScope.Merchant, notification.MerchantId,
                    "listing_count", onSale.Count(), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "上架成就仪表失败（不影响主流程）: MerchantBearingId={Id}", notification.MerchantBearingId);
            }
        }
    }
}
