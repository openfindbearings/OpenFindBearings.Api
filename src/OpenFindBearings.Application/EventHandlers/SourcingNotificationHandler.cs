using MediatR;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.EventHandlers
{
    /// <summary>
    /// 寻货通知订阅者（v1.35.0）：四类事件统一转站内信。
    /// 商户侧通知发给该商户全部在职管理员（与成员/邀请通知同口径）；
    /// 通知类型统一 sourcing（前端一套图标映射），文案区分场景
    /// </summary>
    public class SourcingNotificationHandler :
        INotificationHandler<SourcingRespondedEvent>,
        INotificationHandler<SourcingDemandClosedEvent>,
        INotificationHandler<SourcingDemandCancelledEvent>,
        INotificationHandler<SourcingDemandTakenDownEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly IMerchantMemberRepository _memberRepository;
        private readonly ISourcingResponseRepository _responseRepository;

        /// <summary>
        /// 构造：通知服务 + 成员/应答仓储（找商户管理员与落选应答者）
        /// </summary>
        public SourcingNotificationHandler(
            INotificationService notificationService,
            IMerchantMemberRepository memberRepository,
            ISourcingResponseRepository responseRepository)
        {
            _notificationService = notificationService;
            _memberRepository = memberRepository;
            _responseRepository = responseRepository;
        }

        /// <summary>
        /// 新应答 → 通知发布方：商户名义发布时通知该商户**全部在职成员**（接单不漏人，站内信必发 + 外部按各自偏好）；
        /// 个人名义时仅通知发布人本人（附商户名与型号，引导回详情比价）
        /// </summary>
        public async Task Handle(SourcingRespondedEvent notification, CancellationToken cancellationToken)
        {
            // 改动说明（v1.6.0 通知分发）：商户名义发布（PublisherMerchantId 非空）改发全店在职成员，
            //   不再只发经办人；个人名义维持仅通知发布人
            if (notification.PublisherMerchantId.HasValue)
            {
                await NotifyMerchantMembersAsync(notification.PublisherMerchantId.Value,
                    "寻货收到新应答",
                    $"商户「{notification.MerchantName}」应对了贵店寻货「{notification.PartNumber}」，可查看报价并选定合作。",
                    notification.DemandId, cancellationToken);
                return;
            }
            await _notificationService.AddInAppAsync(
                notification.PublisherUserId,
                Notification.TypeSourcing,
                "寻货收到新应答",
                $"商户「{notification.MerchantName}」应对了您的寻货「{notification.PartNumber}」，可查看报价并选定合作。",
                Notification.BizSourcingDemand, notification.DemandId, cancellationToken);
        }

        /// <summary>
        /// 选定关闭 → 被选商户管理员收"应答被选定"（可联系需求方）；
        /// 其余应答商户收"需求已关闭"（引导查看结果）
        /// </summary>
        public async Task Handle(SourcingDemandClosedEvent notification, CancellationToken cancellationToken)
        {
            await NotifyMerchantAdminsAsync(notification.SelectedMerchantId,
                Notification.TypeSourcing,
                "寻货应答被选定",
                $"您对「{notification.PartNumber}」的应答已被选定，可在寻货详情联系需求方。",
                notification.DemandId, cancellationToken);

            var responses = await _responseRepository.GetByDemandAsync(notification.DemandId, cancellationToken);
            foreach (var lost in responses.Where(r => r.Status == SourcingResponseStatus.NotSelected))
            {
                await NotifyMerchantAdminsAsync(lost.MerchantId,
                    Notification.TypeSourcing,
                    "寻货已结束",
                    $"寻货「{notification.PartNumber}」已由其他商户承接，感谢您的应答。",
                    notification.DemandId, cancellationToken);
            }
        }

        /// <summary>
        /// 发布人取消 → 通知全体待处理应答商户
        /// </summary>
        public async Task Handle(SourcingDemandCancelledEvent notification, CancellationToken cancellationToken)
        {
            var pendings = await _responseRepository.GetPendingByDemandAsync(notification.DemandId, cancellationToken);
            foreach (var pending in pendings)
            {
                await NotifyMerchantAdminsAsync(pending.MerchantId,
                    Notification.TypeSourcing,
                    "寻货已取消",
                    $"您应答的寻货「{notification.PartNumber}」已被发布人取消。",
                    notification.DemandId, cancellationToken);
            }
        }

        /// <summary>
        /// Admin 下架 → 通知发布人（附原因，可申诉口径留白）
        /// </summary>
        public async Task Handle(SourcingDemandTakenDownEvent notification, CancellationToken cancellationToken)
        {
            var reason = string.IsNullOrWhiteSpace(notification.Reason) ? "内容不符合社区规范" : notification.Reason;
            await _notificationService.AddInAppAsync(
                notification.PublisherUserId,
                Notification.TypeSourcing,
                "寻货已被下架",
                $"您的寻货「{notification.PartNumber}」已被平台下架：{reason}。如有疑问可联系在线客服。",
                Notification.BizSourcingDemand, notification.DemandId, cancellationToken);
        }

        /// <summary>
        /// 向商户全部在职管理员发站内信（复用成员表判定，与邀请/关店通知同模式）
        /// </summary>
        private async Task NotifyMerchantAdminsAsync(Guid merchantId, string type, string title, string body,
            Guid bizId, CancellationToken cancellationToken)
        {
            var admins = await _memberRepository.GetActiveByMerchantAsync(merchantId, cancellationToken);
            foreach (var admin in admins.Where(m => m.Role == MerchantMember.RoleMerchantAdmin))
            {
                await _notificationService.AddInAppAsync(admin.UserId, type, title, body,
                    Notification.BizSourcingDemand, bizId, cancellationToken);
            }
        }

        /// <summary>
        /// 向商户全部在职成员（不限角色）发提醒：站内信必发 + 外部通道按各人偏好外发
        /// （v1.6.0 商户名义发布寻货的应答通知口径——接单不漏人）
        /// </summary>
        private async Task NotifyMerchantMembersAsync(Guid merchantId, string title, string body,
            Guid bizId, CancellationToken cancellationToken)
        {
            var members = await _memberRepository.GetActiveByMerchantAsync(merchantId, cancellationToken);
            foreach (var m in members)
            {
                await _notificationService.AddInAppAsync(m.UserId, Notification.TypeSourcing, title, body,
                    Notification.BizSourcingDemand, bizId, cancellationToken);
                await _notificationService.AddExternalAsync(m.UserId, title, body, cancellationToken);
            }
        }
    }
}
