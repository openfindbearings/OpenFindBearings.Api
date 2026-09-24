using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Events;

namespace OpenFindBearings.Application.EventHandlers
{
    /// <summary>
    /// 纠错采纳积分处理器（v1.32.0 积分底座）：兑现 CorrectionProcessedNotificationHandler
    /// 注释预留的扩展点——订阅同一事件，仅采纳分支发分，bizId 按纠错 ID 幂等（重复审批不重发）。
    /// 与通知处理器分离：各订阅者单一职责，发分失败不影响站内信送达
    /// </summary>
    public class CorrectionAdoptedPointsHandler : INotificationHandler<CorrectionProcessedEvent>
    {
        private readonly ILogger<CorrectionAdoptedPointsHandler> _logger;
        private readonly IPointsService _pointsService;

        /// <summary>
        /// 构造：注入积分服务
        /// </summary>
        public CorrectionAdoptedPointsHandler(ILogger<CorrectionAdoptedPointsHandler> logger, IPointsService pointsService)
        {
            _logger = logger;
            _pointsService = pointsService;
        }

        /// <summary>
        /// 采纳后向提交人发放"纠错被采纳"积分（驳回不发）
        /// </summary>
        public async Task Handle(CorrectionProcessedEvent notification, CancellationToken cancellationToken)
        {
            if (!notification.Approved)
                return;

            var granted = await _pointsService.GrantAsync(
                notification.SubmittedBy,
                PointTransaction.TypeCorrectionAdopted,
                $"correction:{notification.CorrectionId:N}",
                null,
                cancellationToken);

            _logger.LogInformation("纠错采纳积分: CorrectionId={CorrectionId}, User={UserId}, Granted={Granted}",
                notification.CorrectionId, notification.SubmittedBy, granted);
        }
    }
}
