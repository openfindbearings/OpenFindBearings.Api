using MediatR;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Events;

namespace OpenFindBearings.Application.EventHandlers
{
    /// <summary>
    /// 提名已被接受事件处理器：向发起人发站内信（你将默认以员工身份入伙该商户）
    /// </summary>
    public class NominationAcceptedNotificationHandler : INotificationHandler<NominationAcceptedEvent>
    {
        private readonly INotificationService _notificationService;

        public NominationAcceptedNotificationHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task Handle(NominationAcceptedEvent notification, CancellationToken cancellationToken)
        {
            await _notificationService.AddInAppAsync(
                notification.InitiatorUserId,
                Notification.TypeNominationAccepted,
                "提名已被接受",
                $"你提名的商户「{notification.MerchantName}」管理员邀请已被接受，资料已提交平台审核，你将以员工身份入伙。",
                Notification.BizMerchant,
                notification.MerchantId,
                cancellationToken);
        }
    }
}
