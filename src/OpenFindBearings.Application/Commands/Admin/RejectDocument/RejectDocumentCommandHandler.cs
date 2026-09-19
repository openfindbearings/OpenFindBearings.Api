using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Admin.RejectDocument
{
    public class RejectDocumentCommandHandler : IRequestHandler<RejectDocumentCommand>
    {
        private readonly IMerchantDocumentRepository _documentRepository;
        private readonly ILogger<RejectDocumentCommandHandler> _logger;

        public RejectDocumentCommandHandler(
            IMerchantDocumentRepository documentRepository,
            ILogger<RejectDocumentCommandHandler> logger)
        {
            _documentRepository = documentRepository;
            _logger = logger;
        }

        public async Task Handle(RejectDocumentCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("审核拒绝营业执照: VerificationId={VerificationId}, ReviewedBy={ReviewedBy}, Reason={Reason}",
                request.VerificationId, request.ReviewedBy, request.Reason);

            var verification = await _documentRepository.GetByIdAsync(request.VerificationId, cancellationToken);
            if (verification == null)
                throw new InvalidOperationException($"审核记录不存在: {request.VerificationId}");

            verification.Reject(request.ReviewedBy, request.Reason);
            await _documentRepository.UpdateAsync(verification, cancellationToken);

            _logger.LogInformation("营业执照审核拒绝: VerificationId={VerificationId}", request.VerificationId);
        }
    }
}
