using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.EventHandlers
{
    /// <summary>
    /// 商户入驻审核通过事件处理器：向该商户全部在职管理员发站内信
    /// 改动说明：订阅发生在业务事务提交后（UnitOfWorkBehavior 时序），通知写入失败仅记日志不回滚审核结果
    /// </summary>
    public class MerchantApprovedNotificationHandler : INotificationHandler<MerchantApprovedEvent>
    {
        private readonly ILogger<MerchantApprovedNotificationHandler> _logger;
        private readonly INotificationService _notificationService;
        private readonly IMerchantMemberRepository _merchantMemberRepository;

        public MerchantApprovedNotificationHandler(
            ILogger<MerchantApprovedNotificationHandler> logger,
            INotificationService notificationService,
            IMerchantMemberRepository merchantMemberRepository)
        {
            _logger = logger;
            _notificationService = notificationService;
            _merchantMemberRepository = merchantMemberRepository;
        }

        public async Task Handle(MerchantApprovedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("商户入驻审核通过: MerchantId={MerchantId}", notification.MerchantId);

            var admins = await _merchantMemberRepository.GetActiveByMerchantAsync(notification.MerchantId, cancellationToken);
            foreach (var member in admins.Where(m => m.Role == MerchantMember.RoleMerchantAdmin))
            {
                await _notificationService.AddInAppAsync(
                    member.UserId,
                    Notification.TypeMerchantApproved,
                    "入驻申请已通过",
                    $"恭喜！商户「{notification.MerchantName}」的入驻申请已审核通过，现在可以开始上架经营了。",
                    Notification.BizMerchant,
                    notification.MerchantId,
                    cancellationToken);
            }
        }
    }
}
