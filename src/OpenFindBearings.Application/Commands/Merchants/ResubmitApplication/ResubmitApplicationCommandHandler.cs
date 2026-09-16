using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Application.Commands.Merchants.ResubmitApplication
{
    /// <summary>
    /// 被拒后修改资料重新提交处理器（v2.6.0 新增）。
    /// 守卫链：商户存在 → Suspended → 调用者为在职 MerchantAdmin → 渠道为 Self/Claim。
    /// 名称/信用代码查重排除自身后与新建同口径（撞他人记录直接 400，不引导改认领——
    /// 编辑页语义是"改自己这张单"，改名将产生新主体，交由用户自行决策）。
    /// </summary>
    public class ResubmitApplicationCommandHandler : IRequestHandler<ResubmitApplicationCommand>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly ILicenseVerificationRepository _licenseRepository;
        private readonly ILogger<ResubmitApplicationCommandHandler> _logger;

        public ResubmitApplicationCommandHandler(
            IMerchantRepository merchantRepository,
            IMerchantMemberRepository merchantMemberRepository,
            ILicenseVerificationRepository licenseRepository,
            ILogger<ResubmitApplicationCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _merchantMemberRepository = merchantMemberRepository;
            _licenseRepository = licenseRepository;
            _logger = logger;
        }

        public async Task Handle(ResubmitApplicationCommand request, CancellationToken cancellationToken)
        {
            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken);
            if (merchant == null)
            {
                throw new InvalidOperationException("要重新提交的申请不存在");
            }

            // 仅被驳回（Suspended）的申请可重提；审核中走撤回、生效后属正常商户管理
            if (merchant.Status != MerchantStatus.Suspended)
            {
                throw new InvalidOperationException("仅被驳回的入驻申请可以修改后重新提交");
            }

            // 重提者必须是该商户当前在职管理员（申请提交时唯一的管理员即申请人本人）
            var member = await _merchantMemberRepository.GetActiveByUserAndMerchantAsync(
                request.ApplicantUserId, request.MerchantId, cancellationToken);
            if (member == null || !member.IsAdmin)
            {
                throw new InvalidOperationException("无权重新提交该申请");
            }

            // 渠道守卫与 withdraw 同口径：提名重提走"接受提名"原通道，None 为历史/爬虫数据不开放
            if (merchant.ApplicationMode is not (ApplicationMode.Self or ApplicationMode.Claim))
            {
                throw new InvalidOperationException("该申请暂不支持自助重新提交");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new InvalidOperationException("商家名称不能为空");
            }
            // 与 apply 同口径：企业名称必填防绕过（重提正是被"资料不完整"驳回的高频场景）
            if (string.IsNullOrWhiteSpace(request.CompanyName))
            {
                throw new InvalidOperationException("企业名称（营业执照全称）不能为空");
            }

            await EnsureNoDuplicateExcludingSelfAsync(merchant, request, cancellationToken);

            // 字段级合并更新（未编辑项保留原值），再回 Pending 重走审核
            merchant.UpdateBasicInfo(
                companyName: request.CompanyName,
                unifiedSocialCreditCode: request.UnifiedSocialCreditCode ?? merchant.UnifiedSocialCreditCode,
                description: request.Description ?? merchant.Description,
                businessScope: merchant.BusinessScope,
                logoUrl: merchant.LogoUrl,
                website: merchant.Website);

            var c = merchant.Contact;
            merchant.UpdateContact(new ContactInfo(
                request.ContactPerson ?? c?.ContactPerson,
                request.Phone ?? c?.Phone,
                request.Mobile ?? c?.Mobile,
                request.Email ?? c?.Email,
                request.Address ?? c?.Address));

            if (request.Type.HasValue)
            {
                merchant.UpdateType((MerchantType)request.Type.Value);
            }

            // 名称允许修改（此前无更新路径，被"名称驳回"的申请无法纠错）
            var trimmedName = request.Name.Trim();
            if (trimmedName != merchant.Name)
            {
                merchant.UpdateName(trimmedName);
            }

            merchant.Resubmit();
            await _merchantRepository.UpdateAsync(merchant, cancellationToken);

            // 可选随附营业执照（与 apply 一致：追加一条记录供后续认证，不影响入驻状态）
            if (!string.IsNullOrWhiteSpace(request.LicenseUrl))
            {
                var verification = new LicenseVerification(merchant.Id, request.LicenseUrl, request.ApplicantUserId);
                await _licenseRepository.AddAsync(verification, cancellationToken);
            }

            _logger.LogInformation("被拒入驻申请已重新提交: MerchantId={MerchantId}, Mode={Mode}, Applicant={UserId}",
                merchant.Id, merchant.ApplicationMode, request.ApplicantUserId);
        }

        /// <summary>
        /// 查重（排除自身）：优先信用代码精确、其次名称精确（均非 Draft），命中他人记录直接拒绝重提。
        /// </summary>
        private async Task EnsureNoDuplicateExcludingSelfAsync(
            Domain.Aggregates.Merchant self,
            ResubmitApplicationCommand request,
            CancellationToken cancellationToken)
        {
            var trimmedName = request.Name!.Trim();
            var creditCode = string.IsNullOrWhiteSpace(request.UnifiedSocialCreditCode)
                ? null
                : request.UnifiedSocialCreditCode!.Trim();

            var duplicate = creditCode != null
                ? await _merchantRepository.GetByCreditCodeAsync(creditCode, cancellationToken)
                : null;
            duplicate ??= await _merchantRepository.GetByNameAsync(trimmedName, cancellationToken);

            // 命中自己（未改名/改代码的正常重提）放行
            if (duplicate == null || duplicate.Id == self.Id) return;

            throw new InvalidOperationException("该商户名称或统一社会信用代码已被其他商家占用，请修改后再提交");
        }
    }
}
