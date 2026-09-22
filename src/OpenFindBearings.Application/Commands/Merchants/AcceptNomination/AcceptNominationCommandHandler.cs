using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Commands.Merchants.ApplicationCleanup;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Merchants.AcceptNomination
{
    /// <summary>
    /// 接受管理员提名命令处理器
    /// 校验邀请有效且未过期，补全商户资料并转为 Pending（Draft -> Pending）
    /// </summary>
    public class AcceptNominationCommandHandler : IRequestHandler<AcceptNominationCommand, Guid>
    {
        private readonly IStaffInvitationRepository _invitationRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMerchantDocumentRepository _documentRepository;
        // 改动说明（v2.16.0）：提名已有商家接管重置需清商品关联，注入仓储
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly IMediator _mediator;
        private readonly ILogger<AcceptNominationCommandHandler> _logger;

        public AcceptNominationCommandHandler(
            IStaffInvitationRepository invitationRepository,
            IMerchantRepository merchantRepository,
            IMerchantDocumentRepository documentRepository,
            IMerchantBearingRepository merchantBearingRepository,
            IUserRepository userRepository,
            IMerchantMemberRepository merchantMemberRepository,
            IMediator mediator,
            ILogger<AcceptNominationCommandHandler> logger)
        {
            _invitationRepository = invitationRepository;
            _merchantRepository = merchantRepository;
            _documentRepository = documentRepository;
            _merchantBearingRepository = merchantBearingRepository;
            _userRepository = userRepository;
            _merchantMemberRepository = merchantMemberRepository;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Guid> Handle(AcceptNominationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("接受管理员提名: Code={Code}, Nominee={NomineeUserId}",
                request.InvitationCode, request.NomineeUserId);

            if (request.NomineeUserId == Guid.Empty)
            {
                throw new InvalidOperationException("请先登录后再接受提名");
            }

            var invitation = await _invitationRepository.GetByCodeAsync(request.InvitationCode, cancellationToken);
            if (invitation == null || invitation.Type != InvitationType.Nomination)
            {
                throw new InvalidOperationException("提名邀请无效");
            }
            if (invitation.Status != InvitationStatus.Pending)
            {
                throw new InvalidOperationException("提名邀请已处理，无法重复接受");
            }
            if (invitation.IsExpired())
            {
                invitation.MarkExpired();
                await _invitationRepository.UpdateAsync(invitation, cancellationToken);
                throw new InvalidOperationException("提名邀请已过期，请联系发起人重新提名");
            }

            // 安全强化：邀请指定了手机号时，仅该手机号的登录用户可接受（防邀请码泄露被冒领）
            if (!string.IsNullOrWhiteSpace(invitation.Phone) &&
                !string.Equals(invitation.Phone, request.NomineePhone, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("该提名邀请仅限被提名人本人接受");
            }

            var merchant = await _merchantRepository.GetByIdAsync(invitation.MerchantId, cancellationToken);
            if (merchant == null)
            {
                throw new InvalidOperationException("提名商户不存在");
            }

            // 改动说明：区分两种提名目标——
            //   Draft（提名新建）：接受即补资料并 Draft→Pending；
            //   已有未认证商家（提名认领已有）：接受=预认领，保持其原状态（不 Draft→Pending）。
            //   两种情况的成员行都由审核通过时统一创建（ApproveMerchant.CreateNominationMembersAsync）。
            var isDraftNomination = merchant.Status == MerchantStatus.Draft;
            if (isDraftNomination)
            {
                // 新建提名走 Draft→Pending，无需额外校验归属
            }
            else
            {
                // 已有商家提名：接受时再校验其仍未被认领/未认证（提名发出后状态可能已变动）
                if (merchant.IsVerified)
                {
                    throw new InvalidOperationException("该商家已认证，无法接受提名");
                }
                var activeMembers = await _merchantMemberRepository.GetActiveByMerchantAsync(merchant.Id, cancellationToken);
                if (activeMembers.Count > 0)
                {
                    throw new InvalidOperationException("该商家已被他人认领");
                }

                // 改动说明（v2.16.0）：提名已有商家=预认领语义，与认领同步做接管重置——
                //   清互联网来源在售商品、历史被拒认领人的旧证照、待确认邀请；
                //   Draft 提名（新建）不清（发起人随单材料属本次申请，非前任遗留）
                await ApplicantApplicationCleanup.ResetOperationalDataForTakeoverAsync(
                    merchant.Id, _merchantBearingRepository, _documentRepository, _invitationRepository, cancellationToken);
            }

            // 改动说明：接受提名补资料时企业名称必填（与 ApplyMerchant 同口径）——
            //   合并后仍为空才拒绝，提名新建时发起人已代填过企业名称的情况允许直接接受
            if (string.IsNullOrWhiteSpace(request.CompanyName) && string.IsNullOrWhiteSpace(merchant.CompanyName))
            {
                throw new InvalidOperationException("企业名称（营业执照全称）不能为空");
            }

            // 修复 B3：补资料场景做字段级合并（?? 原值），
            // 被提名人未填的字段不清空提名（Draft）阶段已填的值
            merchant.UpdateBasicInfo(
                companyName: request.CompanyName ?? merchant.CompanyName,
                unifiedSocialCreditCode: request.UnifiedSocialCreditCode ?? merchant.UnifiedSocialCreditCode,
                description: request.Description ?? merchant.Description,
                businessScope: merchant.BusinessScope,
                logoUrl: merchant.LogoUrl,
                website: merchant.Website);

            // 改动说明（v2.11.0）：合并后信用代码必填（与 apply/resubmit 同口径）
            var creditCodeError = Application.DTOs.DocumentRequirements.ValidateCreditCode(merchant.UnifiedSocialCreditCode);
            if (creditCodeError != null)
            {
                throw new InvalidOperationException(creditCodeError);
            }

            if (!string.IsNullOrWhiteSpace(request.ContactPerson) ||
                !string.IsNullOrWhiteSpace(request.Phone) ||
                !string.IsNullOrWhiteSpace(request.Mobile) ||
                !string.IsNullOrWhiteSpace(request.Email) ||
                !string.IsNullOrWhiteSpace(request.Address))
            {
                var c = merchant.Contact;
                merchant.UpdateContact(new OpenFindBearings.Domain.ValueObjects.ContactInfo(
                    request.ContactPerson ?? c?.ContactPerson,
                    request.Phone ?? c?.Phone,
                    request.Mobile ?? c?.Mobile,
                    request.Email ?? c?.Email,
                    request.Address ?? c?.Address));
            }

            // 改动说明：接受提名=进入真人维护链路，来源置 Manual 使其不再被 Sync 爬虫覆盖
            merchant.SetDataSource(OpenFindBearings.Domain.ValueObjects.DataSource.FromManual(request.NomineeUserId.ToString()));

            // 仅"提名新建"需 Draft→Pending；"提名认领已有商家"保持其原状态（本就非 Draft）
            if (isDraftNomination)
            {
                merchant.SubmitForApproval();
            }
            await _merchantRepository.UpdateAsync(merchant, cancellationToken);

            // 记录被提名人 sub（审核通过时据此建管理员成员行）
            var nomineeUser = await _userRepository.GetByIdAsync(request.NomineeUserId, cancellationToken);
            if (nomineeUser == null)
            {
                throw new InvalidOperationException("登录用户不存在");
            }
            invitation.Complete(nomineeUser.AuthUserId);
            await _invitationRepository.UpdateAsync(invitation, cancellationToken);

            // 改动说明：提名接受成功后即时发布事件，订阅者向发起人发站内信。
            //   此处直接 Publish 而非走实体领域事件（StaffInvitation 无此语义方法，且命令层已知双方身份）；
            //   订阅者内部独立 SaveChanges 落通知，失败只记日志不影响接受结果
            if (invitation.OperatorId != Guid.Empty)
            {
                await _mediator.Publish(new NominationAcceptedEvent(invitation.OperatorId, merchant.Id, merchant.Name), cancellationToken);
            }

            // 改动说明（v2.7.0）：补资料时按材料矩阵校验并落待审记录（与 apply 同口径，提名渠道不豁免）
            var documentError = Application.DTOs.DocumentRequirements.Validate(merchant.Type, request.Documents);
            if (documentError != null)
            {
                throw new InvalidOperationException(documentError);
            }
            foreach (var doc in request.Documents ?? [])
            {
                await _documentRepository.AddAsync(
                    new OpenFindBearings.Domain.Entities.MerchantDocument(
                        merchant.Id, doc.Type, doc.FileUrl.Trim(), request.NomineeUserId),
                    cancellationToken);
            }

            _logger.LogInformation("管理员提名已接受: MerchantId={MerchantId}, Code={Code}", merchant.Id, request.InvitationCode);
            return merchant.Id;
        }
    }
}
