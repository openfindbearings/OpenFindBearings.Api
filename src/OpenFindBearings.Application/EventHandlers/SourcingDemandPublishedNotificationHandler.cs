using MediatR;
using OpenFindBearings.Application.Commands.Sourcing;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.EventHandlers
{
    /// <summary>
    /// 寻货发布通知订阅者（v1.6.0 搜索寻货联动）。
    /// 广播单：匹配"在售本轴承（公域可见）的认证商家"，向其管理员推提醒；
    ///   改动说明（v1.7.0 匹配口径统一）：匹配逻辑改走 SourcingMatcher，与发布处理器回传的"已匹配家数"同口径。
    /// 定向单：仅推目标商户管理员。
    /// 多方式提醒：站内信必发 + 外部通道（短信/微信/邮件，按偏好，网关预留）。
    /// 限频：同一管理员、同型号每日至多 N 条（SystemConfig Sourcing.NotifyMaxPerMerchantPerDay，默认 3），
    ///   超量不再弹信（该需求仍在商家侧"为你匹配的寻货"聚合面可见）。
    /// </summary>
    public class SourcingDemandPublishedNotificationHandler : INotificationHandler<SourcingDemandPublishedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly INotificationRepository _notificationRepository;
        private readonly IMerchantMemberRepository _memberRepository;
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly ISystemConfigRepository _configRepository;
        // 改动说明（v1.6.0 兜底匹配）：无 bearingId（发现页直接发布）时按型号文本精确匹配轴承主数据
        private readonly IBearingRepository _bearingRepository;

        public SourcingDemandPublishedNotificationHandler(
            INotificationService notificationService,
            INotificationRepository notificationRepository,
            IMerchantMemberRepository memberRepository,
            IMerchantBearingRepository merchantBearingRepository,
            IMerchantRepository merchantRepository,
            ISystemConfigRepository configRepository,
            IBearingRepository bearingRepository)
        {
            _notificationService = notificationService;
            _notificationRepository = notificationRepository;
            _memberRepository = memberRepository;
            _merchantBearingRepository = merchantBearingRepository;
            _merchantRepository = merchantRepository;
            _configRepository = configRepository;
            _bearingRepository = bearingRepository;
        }

        /// <inheritdoc/>
        public async Task Handle(SourcingDemandPublishedEvent notification, CancellationToken cancellationToken)
        {
            // 定向单：只推目标商户
            if (notification.TargetMerchantId.HasValue)
            {
                await NotifyMerchantAdminsAsync(notification.TargetMerchantId.Value, notification,
                    "有客户向您定向询价",
                    $"客户定向询价「{notification.PartNumber}」，点此查看并应答报价。", cancellationToken);
                return;
            }

            // 改动说明（v1.7.0 匹配口径统一）：广播单改走 SourcingMatcher（公域可见在售 + 已认证；无 bearingId 时
            //   按型号文本兜底；未命中主数据或无人匹配则谁都不打扰），与发布处理器回传的匹配家数同一口径
            var merchantIds = await SourcingMatcher.GetMatchedMerchantIdsAsync(
                _bearingRepository, _merchantBearingRepository, _merchantRepository,
                notification.BearingId, notification.PartNumber, cancellationToken);

            foreach (var merchantId in merchantIds)
            {
                await NotifyMerchantAdminsAsync(merchantId, notification,
                    "有买家寻货，您的在售型号匹配",
                    $"有买家正在寻货「{notification.PartNumber}」，您的在售型号匹配，点此应答报价。",
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