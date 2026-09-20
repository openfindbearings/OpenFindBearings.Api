using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Merchants.RequestVerifyMerchant
{
    /// <summary>
    /// 申请认证命令处理器（v2.9.0）
    /// 校验链：商户存在 → 已生效（Pending 走入驻审批通道）→ 未认证 → 必备材料全部已批准
    ///   （与 VerifyMerchantCommandHandler 同口径，杜绝"申请了但审不了"的空转）；
    /// 幂等：重复申请直接成功
    /// </summary>
    public class RequestVerifyMerchantCommandHandler : IRequestHandler<RequestVerifyMerchantCommand>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMerchantDocumentRepository _documentRepository;
        private readonly IMerchantMemberRepository _memberRepository;
        private readonly ILogger<RequestVerifyMerchantCommandHandler> _logger;

        public RequestVerifyMerchantCommandHandler(
            IMerchantRepository merchantRepository,
            IMerchantDocumentRepository documentRepository,
            IMerchantMemberRepository memberRepository,
            ILogger<RequestVerifyMerchantCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _documentRepository = documentRepository;
            _memberRepository = memberRepository;
            _logger = logger;
        }

        public async Task Handle(RequestVerifyMerchantCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("商户申请认证: {MerchantId}", request.MerchantId);

            // 越权防护：申请人必须是该商户在职管理员
            var member = await _memberRepository.GetActiveByUserAndMerchantAsync(
                request.OperatorUserId, request.MerchantId, cancellationToken);
            if (member == null || !member.IsAdmin)
            {
                throw new UnauthorizedAccessException("仅商户管理员可申请认证");
            }

            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken);
            if (merchant == null)
            {
                throw new InvalidOperationException($"商家不存在: {request.MerchantId}");
            }

            if (merchant.Status != MerchantStatus.Active)
            {
                throw new InvalidOperationException("商户尚未生效，请先完成入驻审批");
            }

            if (merchant.IsVerified)
            {
                throw new InvalidOperationException("商户已认证，无需重复申请");
            }

            // 资格校验与 Admin 认证同口径：该类型必备材料须全部审核通过
            var documents = await _documentRepository.GetByMerchantIdAsync(merchant.Id, cancellationToken);
            var missing = Application.DTOs.DocumentRequirements.RequiredTypes(merchant.Type)
                .Where(t => !documents.Any(d => d.Type == t && d.Status == DocumentStatus.Approved))
                .Select(Application.DTOs.DocumentRequirements.DisplayName)
                .ToList();
            if (missing.Count > 0)
            {
                throw new InvalidOperationException($"缺少已审核通过的必备材料：{string.Join("、", missing)}，请先在信息维护补传");
            }

            // 幂等：已申请过再次提交不报错
            if (!merchant.VerifyRequested)
            {
                merchant.RequestVerify();
                await _merchantRepository.UpdateAsync(merchant, cancellationToken);
            }

            _logger.LogInformation("商户申请认证已受理: {MerchantId}", merchant.Id);
        }
    }
}
