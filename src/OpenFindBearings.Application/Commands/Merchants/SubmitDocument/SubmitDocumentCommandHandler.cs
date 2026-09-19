using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Merchants.SubmitDocument
{
    /// <summary>
    /// 提交商户证照材料审核命令处理器（v2.7.0：泛化多类型；入驻后换证/补材料独立通道，
    /// 随入驻申请提交的材料由 Apply/Resubmit/Accept 处理器直接落库，不经此命令）。
    /// 改动说明：由"导航集合 Add + 根 UpdateAsync"改为显式仓储 AddAsync——
    ///   规避 BaseEntity 预生成 Guid 经导航被 EF 判 Modified 的并发回滚陷阱（见 AGENTS.md EF 陷阱条目）。
    /// </summary>
    public class SubmitDocumentCommandHandler : IRequestHandler<SubmitDocumentCommand, Guid>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMerchantDocumentRepository _documentRepository;
        private readonly ILogger<SubmitDocumentCommandHandler> _logger;

        public SubmitDocumentCommandHandler(
            IMerchantRepository merchantRepository,
            IMerchantDocumentRepository documentRepository,
            ILogger<SubmitDocumentCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _documentRepository = documentRepository;
            _logger = logger;
        }

        public async Task<Guid> Handle(SubmitDocumentCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("提交证照材料审核: MerchantId={MerchantId}, Type={Type}, SubmittedBy={SubmittedBy}",
                request.MerchantId, request.Type, request.SubmittedBy);

            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken);
            if (merchant == null)
            {
                throw new InvalidOperationException($"商家不存在: {request.MerchantId}");
            }

            // 同类材料已有待审记录时防重复提交（营业执照唯一性强约束；授权书/厂房照允许多份但避免同一份反复刷）
            var existing = await _documentRepository.GetByMerchantIdAsync(request.MerchantId, cancellationToken);
            if (request.Type == Domain.Enums.DocumentType.BusinessLicense
                && existing.Any(d => d.Type == Domain.Enums.DocumentType.BusinessLicense
                    && d.Status != Domain.Enums.DocumentStatus.Rejected))
            {
                throw new InvalidOperationException("营业执照已在审核中或已通过，无需重复提交");
            }

            var document = new MerchantDocument(
                request.MerchantId,
                request.Type,
                request.FileUrl.Trim(),
                request.SubmittedBy);

            await _documentRepository.AddAsync(document, cancellationToken);

            _logger.LogInformation("证照材料已提交待审: DocumentId={DocumentId}, MerchantId={MerchantId}",
                document.Id, request.MerchantId);

            return document.Id;
        }
    }
}
