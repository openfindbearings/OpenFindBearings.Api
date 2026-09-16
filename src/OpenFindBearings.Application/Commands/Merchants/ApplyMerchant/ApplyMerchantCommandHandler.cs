using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Exceptions;
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

            // 改动说明：企业名称（营业执照全称）升级为必填（self/claim 统一入口校验）——
            //   审核依据需要主体公司名，此前选填导致审批列表大量空值盲审；前端表单已同步加校验
            if (string.IsNullOrWhiteSpace(request.CompanyName))
            {
                throw new InvalidOperationException("企业名称（营业执照全称）不能为空");
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

            // 改动说明：真人新建前查重，避免与库中已有商户产生重名/同信用代码的重复记录。
            //   命中可认领商户（未认证+无在职成员）→ 抛冲突异常引导前端改为认领；
            //   命中已认证/已被认领商户 → 直接拒绝；无命中才新建。
            await EnsureNoSelfDuplicateAsync(request, cancellationToken);

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

            // 改动说明：真人新建的商户即"人工维护"，来源置 Manual，杜绝被后续爬虫同步覆盖或夺走；
            //   此前 ApplySelf 未设来源(null)，会被 BatchCreateMerchants 视为可覆盖并回填 Crawler（漏洞修复）
            merchant.SetDataSource(DataSource.FromManual("apply-self"));
            // 改动说明：标记入驻渠道为 Self，申请人撤回时据此硬删除商户（见 WithdrawApplicationCommandHandler）
            merchant.MarkApplicationMode(ApplicationMode.Self);

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
        /// 自助新建查重：优先按统一社会信用代码精确匹配，其次按商户名精确匹配（均排除草稿）。
        /// 命中"可认领"商户（未认证且无在职成员）抛 <see cref="MerchantClaimableConflictException"/> 引导改认领；
        /// 命中已认证/已被他人认领商户抛 <see cref="InvalidOperationException"/> 直接拒绝。
        /// </summary>
        private async Task EnsureNoSelfDuplicateAsync(ApplyMerchantCommand request, CancellationToken cancellationToken)
        {
            var trimmedName = request.Name!.Trim();
            var creditCode = string.IsNullOrWhiteSpace(request.UnifiedSocialCreditCode)
                ? null
                : request.UnifiedSocialCreditCode!.Trim();

            var duplicate = creditCode != null
                ? await _merchantRepository.GetByCreditCodeAsync(creditCode, cancellationToken)
                : null;
            duplicate ??= await _merchantRepository.GetByNameAsync(trimmedName, cancellationToken);
            if (duplicate == null) return;

            // 可认领判定与向导第一步/认领门一致：未认证 且 无在职成员
            var claimable = !duplicate.IsVerified;
            if (claimable)
            {
                var activeMembers = await _merchantMemberRepository.GetActiveByMerchantAsync(duplicate.Id, cancellationToken);
                claimable = activeMembers.Count == 0;
            }

            if (claimable)
            {
                _logger.LogInformation("自助新建撞名命中可认领商户，引导改认领: ExistingMerchantId={Id}", duplicate.Id);
                throw new MerchantClaimableConflictException(duplicate.Id, duplicate.Name);
            }

            throw new InvalidOperationException("该商户已存在（同名或同信用代码）且已认证或已被认领，无法重复新建");
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

            // 改动说明：认领门由"来源=Crawler"改为"未认证"——可认领只看 IsVerified（与"是否被爬虫覆盖"的
            //   DataSource 轴解耦）。已认证商家不可认领；未认证的开放商家（含爬虫来源）均可被真人认领。
            if (merchant.IsVerified)
            {
                throw new InvalidOperationException("该商家已认证，无法认领");
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

            // 改动说明：认领即真人接管该商户——应用向导第三步核对/补全的资料（字段级合并，未编辑项
            //   回填原值，避免 UpdateBasicInfo/UpdateContact 全量覆盖清空 logo/website/businessScope），
            //   并置来源为 Manual，使其不再被 Sync 爬虫覆盖（覆盖保护以 DataSource 为键，认领人已接管）。
            merchant.UpdateBasicInfo(
                companyName: request.CompanyName ?? merchant.CompanyName,
                unifiedSocialCreditCode: request.UnifiedSocialCreditCode ?? merchant.UnifiedSocialCreditCode,
                description: request.Description ?? merchant.Description,
                businessScope: merchant.BusinessScope,
                logoUrl: merchant.LogoUrl,
                website: merchant.Website);

            var hasContact = !string.IsNullOrWhiteSpace(request.ContactPerson) ||
                !string.IsNullOrWhiteSpace(request.Phone) ||
                !string.IsNullOrWhiteSpace(request.Mobile) ||
                !string.IsNullOrWhiteSpace(request.Email) ||
                !string.IsNullOrWhiteSpace(request.Address);
            if (hasContact)
            {
                var c = merchant.Contact;
                merchant.UpdateContact(new ContactInfo(
                    request.ContactPerson ?? c?.ContactPerson,
                    request.Phone ?? c?.Phone,
                    request.Mobile ?? c?.Mobile,
                    request.Email ?? c?.Email,
                    request.Address ?? c?.Address));
            }

            merchant.SetDataSource(DataSource.FromManual(request.ApplicantUserId.ToString()));
            // 改动说明：标记入驻渠道为 Claim，申请人撤回时据此仅解除成员+退回爬虫、不删商户本体
            merchant.MarkApplicationMode(ApplicationMode.Claim);

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
