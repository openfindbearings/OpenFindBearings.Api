using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Common.Interfaces;
using OpenFindBearings.Domain.Events;

namespace OpenFindBearings.Application.Features.Bearings.EventHandlers
{
    /// <summary>
    /// 轴承提交审核事件处理器
    /// </summary>
    public class BearingSubmittedForApprovalEventHandler : INotificationHandler<BearingSubmittedForApprovalEvent>
    {
        private readonly ILogger<BearingSubmittedForApprovalEventHandler> _logger;
        private readonly INotificationService _notificationService;

        public BearingSubmittedForApprovalEventHandler(
            ILogger<BearingSubmittedForApprovalEventHandler> logger,
            INotificationService notificationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        public async Task Handle(BearingSubmittedForApprovalEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "轴承提交审核: 商家ID={MerchantId}, 轴承ID={BearingId}, 关联ID={MerchantBearingId}",
                notification.MerchantId,
                notification.BearingId,
                notification.MerchantBearingId);

            // 通知管理员有新待审核产品
            await _notificationService.SendToAdminsAsync(
                "新轴承待审核",
                $"商家添加了新轴承，等待审核",
                cancellationToken);
        }
    }
}
