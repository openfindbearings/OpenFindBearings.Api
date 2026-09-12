using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Application.Commands.Merchants.ApplyMerchant
{
    /// <summary>
    /// 商户入驻申请命令处理器
    /// self：新建 Pending 商户 + 申请人 MerchantAdmin 成员；claim：认领爬虫商家并绑定管理员
    /// </summary>
    public class ApplyMerchantCommandHandler : IRequestHandler<ApplyMerchantCommand, Guid>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly ILicenseVerificationRepository _licenseRepository;
        private readonly ILogger<ApplyMerchantCommandHandler> _logger;

        public ApplyMerchantCommandHandler(
            IMerchantRepository merchantRepository,
            IMerchantMemberRepository merchantMemberRepository,
            ILicenseVerificationRepository licenseRepository,
            ILogger<ApplyMerchantCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _merchantMemberRepository = merchantMemberRepository;
            _licenseRepository = licenseRepository;
            _logger = logger;
        }

        public async Task<Guid> Handle(ApplyMerchantCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("商户入驻申请: Mode={Mode}, Applicant={ApplicantUserId}",
                request.Mode, request.ApplicantUserId);

            if (request.ApplicantUserId == Guid.Empty)
            {
                throw new InvalidOperationException("请先登录后再申请入驻");
            }

            var merchant = request.Mode == "claim"
                ? await ApplyClaimAsync(request, cancellationToken)
                : await ApplySelfAsync(request, cancellationToken);

            // 可选提交营业执照（用于后续认证，不影响入驻生效）
            if (!string.IsNullOrWhiteSpace(request.LicenseUrl))
            {
                var verification = new LicenseVerification(merchant.Id, request.LicenseUrl, request.ApplicantUserId);
                await _licenseRepository.AddAsync(verification, cancellationToken);
                _logger.LogInformation("已随入驻提交营业执照: MerchantId={MerchantId}", merchant.Id);
            }

            return merchant.Id;
        }

        /// <summary>
        /// 模式 self：新建商家（待审核）+ 申请人管理员成员
        /// </summary>
        private async Task<Merchant> ApplySelfAsync(ApplyMerchantCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new InvalidOperationException("商家名称不能为空");
            }

            var contact = new ContactInfo(
                request.ContactPerson,
                request.Phone,
                request.Mobile,
                request.Email,
                request.Address);

            var merchant = new Merchant(
                request.Name.Trim(),
                request.Type.HasValue ? (MerchantType)request.Type.Value : MerchantType.Trader,
                contact);

            merchant.UpdateBasicInfo(
                companyName: request.CompanyName,
                unifiedSocialCreditCode: request.UnifiedSocialCreditCode,
                description: request.Description,
                businessScope: null,
                logoUrl: null,
                website: null);

            await _merchantRepository.AddAsync(merchant, cancellationToken);

            var member = new MerchantMember(
                request.ApplicantUserId,
                merchant.Id,
                MerchantMember.RoleMerchantAdmin);
            await _merchantMemberRepository.AddAsync(member, cancellationToken);

            _logger.LogInformation("新建商家入驻申请: MerchantId={MerchantId}", merchant.Id);
            return merchant;
        }

        /// <summary>
        /// 模式 claim：认领爬虫商家并绑定管理员（仅可认领未认领的爬虫来源商家）
        /// </summary>
        private async Task<Merchant> ApplyClaimAsync(ApplyMerchantCommand request, CancellationToken cancellationToken)
        {
            if (!request.ClaimMerchantId.HasValue || request.ClaimMerchantId.Value == Guid.Empty)
            {
                throw new InvalidOperationException("请选择要认领的商家");
            }

            var merchant = await _merchantRepository.GetByIdAsync(request.ClaimMerchantId.Value, cancellationToken);
            if (merchant == null)
            {
                throw new InvalidOperationException("要认领的商家不存在");
            }

            if (merchant.DataSource == null || !merchant.DataSource.IsCrawler)
            {
                throw new InvalidOperationException("只能认领爬虫来源的商家");
            }

            // 修复 B8：显式检查在职成员，防止并发窗口内重复认领产生双管理员
            var activeMembers = await _merchantMemberRepository.GetActiveByMerchantAsync(merchant.Id, cancellationToken);
            if (activeMembers.Count > 0)
            {
                if (merchant.Status == MerchantStatus.Suspended)
                {
                    // 修复 B7：曾被拒绝的爬虫商家再次被认领——解除旧成员关系（原认领人申请失败），
                    // 商户恢复 Pending 重新走审核；否则原认领人将被"已被认领"永久锁死
                    foreach (var oldMember in activeMembers)
                    {
                        oldMember.Remove();
                        await _merchantMemberRepository.UpdateAsync(oldMember, cancellationToken);
                    }
                    merchant.ReopenForClaim();
                    await _merchantRepository.UpdateAsync(merchant, cancellationToken);
                    _logger.LogInformation("曾被拒的商家重新开放认领，旧成员已解除: MerchantId={MerchantId}", merchant.Id);
                }
                else
                {
                    throw new InvalidOperationException("该商家已被他人认领");
                }
            }

            var member = new MerchantMember(
                request.ApplicantUserId,
                merchant.Id,
                MerchantMember.RoleMerchantAdmin);
            await _merchantMemberRepository.AddAsync(member, cancellationToken);

            _logger.LogInformation("认领爬虫商家: MerchantId={MerchantId}", merchant.Id);
            return merchant;
        }
    }
}
