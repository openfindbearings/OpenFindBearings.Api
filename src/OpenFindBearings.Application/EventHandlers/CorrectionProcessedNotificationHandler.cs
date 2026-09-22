using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Events;

namespace OpenFindBearings.Application.EventHandlers
{
    /// <summary>
    /// 纠错审核结果通知处理器（v2.14.0）：采纳/驳回后向提交人发站内信，
    /// 打通"用户纠错 → 平台处理 → 结果触达"闭环（此前审批完用户无感知）。
    /// 积分奖励扩展点：采纳时给提交人加积分的订阅者待积分体系（P8）落地后新增，
    /// 直接订阅同一 CorrectionProcessedEvent（Approved=true 分支），不改本处理器。
    /// </summary>
    public class CorrectionProcessedNotificationHandler : INotificationHandler<CorrectionProcessedEvent>
    {
        private readonly ILogger<CorrectionProcessedNotificationHandler> _logger;
        private readonly INotificationService _notificationService;

        public CorrectionProcessedNotificationHandler(
            ILogger<CorrectionProcessedNotificationHandler> logger,
            INotificationService notificationService)
        {
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task Handle(CorrectionProcessedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("纠错审核完成通知: CorrectionId={CorrectionId}, Approved={Approved}, User={UserId}",
                notification.CorrectionId, notification.Approved, notification.SubmittedBy);

            var target = notification.TargetType == "Bearing" ? "轴承信息" : "商家信息";
            if (notification.Approved)
            {
                await _notificationService.AddInAppAsync(
                    notification.SubmittedBy,
                    Notification.TypeCorrectionProcessed,
                    "纠错已被采纳",
                    $"您提交的{target}纠错已核实采纳，感谢您帮助平台提升数据准确性！",
                    Notification.BizCorrection,
                    notification.CorrectionId,
                    cancellationToken);
            }
            else
            {
                await _notificationService.AddInAppAsync(
                    notification.SubmittedBy,
                    Notification.TypeCorrectionProcessed,
                    "纠错未采纳",
                    $"您提交的{target}纠错未通过审核。原因：{notification.ReviewComment ?? "信息核实不一致"}。如有补充凭证可再次提交。",
                    Notification.BizCorrection,
                    notification.CorrectionId,
                    cancellationToken);
            }
        }
    }
}
