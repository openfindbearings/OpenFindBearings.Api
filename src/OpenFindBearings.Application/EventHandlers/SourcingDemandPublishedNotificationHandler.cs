using MediatR;
using OpenFindBearings.Application.Commands.Sourcing;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.EventHandlers
{
    /// <summary>
    /// 寻货发布通知订阅者（v1.6.0 搜索寻货联动；v1.8.0 改为按事件携带的收件人集合推送）。
    /// 收件人集合由发布处理器按"公开招标=在售匹配口径 / 邀标=勾选∩匹配"算定后随事件传入，
    /// 本订阅者只负责扇出：公开=匹配文案，邀标=邀请文案；集合为空则谁都不打扰。
    /// 多方式提醒：站内信必发 + 外部通道（短信/微信/邮件，按偏好，网关预留）。
    /// 限频：同一管理员、同型号每日至多 N 条（SystemConfig Sourcing.NotifyMaxPerMerchantPerDay，默认 3），
    ///   超量不再弹信（该需求仍在商家侧"为你匹配的寻货"聚合面可见）。
    /// </summary>
    public class SourcingDemandPublishedNotificationHandler : INotificationHandler<SourcingDemandPublishedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly INotificationRepository _notificationRepository;
        private readonly IMerchantMemberRepository _memberRepository;
        private readonly ISystemConfigRepository _configRepository;

        /// <summary>
        /// 构造：通知服务 + 通知仓储（限频计数）+ 商户成员仓储（取管理员）+ 配置仓储（限频阈值）
        /// </summary>
        public SourcingDemandPublishedNotificationHandler(
            INotificationService notificationService,
            INotificationRepository notificationRepository,
            IMerchantMemberRepository memberRepository,
            ISystemConfigRepository configRepository)
        {
            _notificationService = notificationService;
            _notificationRepository = notificationRepository;
            _memberRepository = memberRepository;
            _configRepository = configRepository;
        }

        /// <inheritdoc/>
        public async Task Handle(SourcingDemandPublishedEvent notification, CancellationToken cancellationToken)
        {
            // 改动说明（v1.8.0 公开招标/邀标）：匹配不再发生在本订阅者内——收件人集合已由发布处理器算定
            //   （SourcingMatcher 唯一调用处），此处只按集合推送，避免"回传家数"与"实际推送集合"口径漂移
            var isInvited = notification.Visibility == SourcingVisibility.Invited;
            foreach (var merchantId in notification.RecipientMerchantIds)
            {
                await NotifyMerchantAdminsAsync(merchantId, notification,
                    isInvited ? "有客户向您发出寻货邀请" : "有买家寻货，您的在售型号匹配",
                    isInvited
                        ? $"客户邀请您对「{notification.PartNumber}」报价，点此查看并应答。"
                        : $"有买家正在寻货「{notification.PartNumber}」，您的在售型号匹配，点此应答报价。",
                    cancellationToken);
            }
        }

        /// <summary>
        /// 向商户全部在职管理员发提醒（站内信必发 + 外部通道按偏好外发），
        /// 并按"同型号每日上限"限频（超量的管理员跳过）。
        /// </summary>
        private async Task NotifyMerchantAdminsAsync(Guid merchantId, SourcingDemandPublishedEvent e,
            string title, string body, CancellationToken cancellationToken)
        {
            var maxPerDay = await SourcingConfigReader.GetIntAsync(
                _configRepository, "Sourcing.NotifyMaxPerMerchantPerDay", 3);

            var admins = await _memberRepository.GetActiveByMerchantAsync(merchantId, cancellationToken);
            foreach (var admin in admins.Where(m => m.Role == MerchantMember.RoleMerchantAdmin))
            {
                // 限频：该管理员今日同型号寻货提醒数达上限则跳过（正文含型号，按正文包含匹配）
                var sentToday = await _notificationRepository.CountTodayByTypeAsync(
                    admin.UserId, Notification.TypeSourcing, e.PartNumber, cancellationToken);
                if (sentToday >= maxPerDay)
                    continue;

                await _notificationService.AddInAppAsync(admin.UserId, Notification.TypeSourcing,
                    title, body, Notification.BizSourcingDemand, e.DemandId, cancellationToken);
                // 多方式提醒：站内信已发，外部通道按偏好扇出（网关预留，未就绪只记日志）
                await _notificationService.AddExternalAsync(admin.UserId, title, body, cancellationToken);
            }
        }
    }
}
