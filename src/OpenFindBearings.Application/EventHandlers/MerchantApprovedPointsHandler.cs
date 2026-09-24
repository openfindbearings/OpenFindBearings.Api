using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.EventHandlers
{
    /// <summary>
    /// 商户入驻通过积分处理器（v1.32.0 积分底座）：审核通过后向入驻人发放一次性奖励。
    /// 入驻人判定=该商户最早加入的在职管理员（审批通过时申请人先建成员行，后续管理员
    /// 均由其邀请加入，创建时间序即身份序）；bizId 按商户幂等，重复审批/多管理员只发一份
    /// </summary>
    public class MerchantApprovedPointsHandler : INotificationHandler<MerchantApprovedEvent>
    {
        private readonly ILogger<MerchantApprovedPointsHandler> _logger;
        private readonly IPointsService _pointsService;
        private readonly IMerchantMemberRepository _memberRepository;

        /// <summary>
        /// 构造：注入积分与成员仓储
        /// </summary>
        public MerchantApprovedPointsHandler(
            ILogger<MerchantApprovedPointsHandler> logger,
            IPointsService pointsService,
            IMerchantMemberRepository memberRepository)
        {
            _logger = logger;
            _pointsService = pointsService;
            _memberRepository = memberRepository;
        }

        /// <summary>
        /// 向最早入伙的在职管理员（入驻人）发放商户入驻通过积分
        /// </summary>
        public async Task Handle(MerchantApprovedEvent notification, CancellationToken cancellationToken)
        {
            var admins = await _memberRepository.GetActiveByMerchantAsync(notification.MerchantId, cancellationToken);
            var applicant = admins
                .Where(m => m.Role == MerchantMember.RoleMerchantAdmin)
                .OrderBy(m => m.CreatedAt)
                .FirstOrDefault();
            if (applicant == null)
                return;

            var granted = await _pointsService.GrantAsync(
                applicant.UserId,
                PointTransaction.TypeMerchantApproved,
                $"merchant_approved:{notification.MerchantId:N}",
                $"商户「{notification.MerchantName}」入驻通过",
                cancellationToken);

            _logger.LogInformation("入驻通过积分: MerchantId={MerchantId}, User={UserId}, Granted={Granted}",
                notification.MerchantId, applicant.UserId, granted);
        }
    }
}
