using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Admin.ApproveDocument
{
    /// <summary>
    /// 审核通过证照材料命令处理器（v2.7.0：仅处理入驻后的材料变更队列）。
    /// 改动说明：移除旧"执照通过即自动认证商家"耦合——认证改由 verify 端点按材料矩阵口径独立判定
    /// </summary>
    public class ApproveDocumentCommandHandler : IRequestHandler<ApproveDocumentCommand>
    {
        private readonly IMerchantDocumentRepository _documentRepository;
        private readonly ILogger<ApproveDocumentCommandHandler> _logger;

        public ApproveDocumentCommandHandler(
            IMerchantDocumentRepository documentRepository,
            ILogger<ApproveDocumentCommandHandler> logger)
        {
            _documentRepository = documentRepository;
            _logger = logger;
        }

        public async Task Handle(ApproveDocumentCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("审核通过证照材料: DocumentId={DocumentId}, ReviewedBy={ReviewedBy}",
                request.VerificationId, request.ReviewedBy);

            var document = await _documentRepository.GetByIdAsync(request.VerificationId, cancellationToken);
            if (document == null)
                throw new InvalidOperationException($"审核记录不存在: {request.VerificationId}");

            if (document.Status != DocumentStatus.Pending)
                throw new InvalidOperationException($"该材料已审核: {request.VerificationId}");

            document.Approve(request.ReviewedBy, request.Comment);
            await _documentRepository.UpdateAsync(document, cancellationToken);

            _logger.LogInformation("证照材料审核通过: DocumentId={DocumentId}, MerchantId={MerchantId}",
                document.Id, document.MerchantId);
        }
    }
}
