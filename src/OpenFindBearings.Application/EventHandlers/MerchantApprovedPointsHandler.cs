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
    /// 均由其邀请加入，创建时间序即身份序）。
    /// 改动说明（v1.34.0 防刷）：bizId 从商户 ID 改绑统一社会信用代码——删店重入驻会生成新
    /// merchantId 导致重复发放，信用代码是商户实体不变标识，同执照永久只发一次；
    /// 无信用代码的历史商户回退 merchantId（存量数据不受影响）
    /// </summary>
    public class MerchantApprovedPointsHandler : INotificationHandler<MerchantApprovedEvent>
    {
        private readonly ILogger<MerchantApprovedPointsHandler> _logger;
        private readonly IPointsService _pointsService;
        private readonly IMerchantMemberRepository _memberRepository;
        private readonly IMerchantRepository _merchantRepository;

        /// <summary>
        /// 构造：注入积分与成员/商户仓储
        /// </summary>
        public MerchantApprovedPointsHandler(
            ILogger<MerchantApprovedPointsHandler> logger,
            IPointsService pointsService,
            IMerchantMemberRepository memberRepository,
            IMerchantRepository merchantRepository)
        {
            _logger = logger;
            _pointsService = pointsService;
            _memberRepository = memberRepository;
            _merchantRepository = merchantRepository;
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

            // 幂等键优先信用代码（防删店重入驻循环），历史无码商户回退商户 ID；
            // v1.34.0：改走认领台账 GrantOneTimeAsync——流水随注销清空后台账仍在，双保险
            var merchant = await _merchantRepository.GetByIdAsync(notification.MerchantId, cancellationToken);
            var claimKey = !string.IsNullOrWhiteSpace(merchant?.UnifiedSocialCreditCode)
                ? $"credit:{merchant.UnifiedSocialCreditCode.Trim().ToUpperInvariant()}:approved"
                : $"merchant:{notification.MerchantId:N}:approved";

            var granted = await _pointsService.GrantOneTimeAsync(
                applicant.UserId,
                PointTransaction.TypeMerchantApproved,
                claimKey,
                $"商户「{notification.MerchantName}」入驻通过",
                cancellationToken);

            _logger.LogInformation("入驻通过积分: MerchantId={MerchantId}, User={UserId}, Granted={Granted}",
                notification.MerchantId, applicant.UserId, granted);
        }
    }
}
