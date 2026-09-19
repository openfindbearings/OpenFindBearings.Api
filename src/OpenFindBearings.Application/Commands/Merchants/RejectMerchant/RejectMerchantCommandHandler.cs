using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Exceptions;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Merchants.RejectMerchant
{
    public class RejectMerchantCommandHandler : IRequestHandler<RejectMerchantCommand>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMerchantDocumentRepository _documentRepository;
        private readonly ILogger<RejectMerchantCommandHandler> _logger;

        public RejectMerchantCommandHandler(
            IMerchantRepository merchantRepository,
            IMerchantDocumentRepository documentRepository,
            ILogger<RejectMerchantCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _documentRepository = documentRepository;
            _logger = logger;
        }

        public async Task Handle(RejectMerchantCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("拒绝商家认证: {MerchantId}, Reason={Reason}", request.Id, request.Reason);

            var merchant = await _merchantRepository.GetByIdAsync(request.Id, cancellationToken);
            if (merchant == null)
            {
                throw new InvalidOperationException($"商家不存在: {request.Id}");
            }

            // 改动说明：并发守卫——另一管理员已先处理时返回 409（与 Approve 对称）
            if (merchant.Status != MerchantStatus.Pending)
            {
                throw new MerchantAlreadyProcessedException(merchant.Status.ToString());
            }

            merchant.Reject(request.Reason);
            await _merchantRepository.UpdateAsync(merchant, cancellationToken);

            // 改动说明（v2.7.0）：随单待审材料级联拒绝，避免旧材料滞留待审队列、并支持重提时"缺什么补什么"判定
            var documents = await _documentRepository.GetByMerchantIdAsync(merchant.Id, cancellationToken);
            foreach (var doc in documents.Where(d => d.Status == DocumentStatus.Pending))
            {
                doc.Reject(request.ReviewedBy ?? Guid.Empty, request.Reason);
                await _documentRepository.UpdateAsync(doc, cancellationToken);
            }

            _logger.LogInformation("商家认证已拒绝: {MerchantId}", request.Id);
        }
    }
}
