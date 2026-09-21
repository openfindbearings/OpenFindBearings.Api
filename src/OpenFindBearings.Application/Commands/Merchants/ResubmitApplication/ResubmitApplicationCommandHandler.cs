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
        private readonly IMerchantDocumentRepository _documentRepository;
        private readonly ILogger<ResubmitApplicationCommandHandler> _logger;

        public ResubmitApplicationCommandHandler(
            IMerchantRepository merchantRepository,
            IMerchantMemberRepository merchantMemberRepository,
            IMerchantDocumentRepository documentRepository,
            ILogger<ResubmitApplicationCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _merchantMemberRepository = merchantMemberRepository;
            _documentRepository = documentRepository;
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

            // 改动说明（v2.11.0）：合并后信用代码必填（与 apply 同口径）——
            //   存量已有有效值的商户不必重填，历史空值商户被拒重提时强制补录
            var creditCodeError = Application.DTOs.DocumentRequirements.ValidateCreditCode(merchant.UnifiedSocialCreditCode);
            if (creditCodeError != null)
            {
                throw new InvalidOperationException(creditCodeError);
            }

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

            // 改动说明（v2.7.0）：材料矩阵校验——已批准材料（前轮审核通过保留）+ 本次新提交合并判定；
            //   上一轮 Pending 材料已随拒绝级联置 Rejected，故缺什么补什么，防止"空材料重提"绕过审核
            var approvedSoFar = (await _documentRepository.GetByMerchantIdAsync(merchant.Id, cancellationToken))
                .Where(d => d.Status == DocumentStatus.Approved)
                .Select(d => new Application.DTOs.DocumentSubmission(d.Type, d.FileUrl))
                .ToList();
            var newDocs = request.Documents ?? [];
            var documentError = Application.DTOs.DocumentRequirements.Validate(
                merchant.Type, [.. approvedSoFar, .. newDocs]);
            if (documentError != null)
            {
                throw new InvalidOperationException(documentError);
            }

            // 名称允许修改（此前无更新路径，被"名称驳回"的申请无法纠错）
            var trimmedName = request.Name.Trim();
            if (trimmedName != merchant.Name)
            {
                merchant.UpdateName(trimmedName);
            }

            merchant.Resubmit();
            await _merchantRepository.UpdateAsync(merchant, cancellationToken);

            // 改动说明（v2.7.0）：本次新提交的材料逐条建待审记录（原单一执照泛化为多类型集合）
            foreach (var doc in newDocs)
            {
                await _documentRepository.AddAsync(
                    new MerchantDocument(merchant.Id, doc.Type, doc.FileUrl.Trim(), request.ApplicantUserId),
                    cancellationToken);
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
