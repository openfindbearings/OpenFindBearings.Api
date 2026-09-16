using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.EventHandlers
{
    /// <summary>
    /// 商户入驻审核拒绝事件处理器：向该商户全部在职管理员发站内信（正文含拒绝原因）
    /// </summary>
    public class MerchantRejectedNotificationHandler : INotificationHandler<MerchantRejectedEvent>
    {
        private readonly ILogger<MerchantRejectedNotificationHandler> _logger;
        private readonly INotificationService _notificationService;
        private readonly IMerchantMemberRepository _merchantMemberRepository;

        public MerchantRejectedNotificationHandler(
            ILogger<MerchantRejectedNotificationHandler> logger,
            INotificationService notificationService,
            IMerchantMemberRepository merchantMemberRepository)
        {
            _logger = logger;
            _notificationService = notificationService;
            _merchantMemberRepository = merchantMemberRepository;
        }

        public async Task Handle(MerchantRejectedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("商户入驻审核拒绝: MerchantId={MerchantId}, Reason={Reason}",
                notification.MerchantId, notification.Reason);

            var admins = await _merchantMemberRepository.GetActiveByMerchantAsync(notification.MerchantId, cancellationToken);
            foreach (var member in admins.Where(m => m.Role == MerchantMember.RoleMerchantAdmin))
            {
                await _notificationService.AddInAppAsync(
                    member.UserId,
                    Notification.TypeMerchantRejected,
                    "入驻申请被拒绝",
                    $"商户「{notification.MerchantName}」的入驻申请未通过审核。原因：{notification.Reason}。可修改资料后重新提交。",
                    Notification.BizMerchant,
                    notification.MerchantId,
                    cancellationToken);
            }
        }
    }
}
