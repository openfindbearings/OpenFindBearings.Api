using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Admin.Commands;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Admin.Handlers
{
    public class RejectLicenseCommandHandler : IRequestHandler<RejectLicenseCommand>
    {
        private readonly ILicenseVerificationRepository _licenseRepository;
        private readonly ILogger<RejectLicenseCommandHandler> _logger;

        public RejectLicenseCommandHandler(
            ILicenseVerificationRepository licenseRepository,
            ILogger<RejectLicenseCommandHandler> logger)
        {
            _licenseRepository = licenseRepository;
            _logger = logger;
        }

        public async Task Handle(RejectLicenseCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("审核拒绝营业执照: VerificationId={VerificationId}, ReviewedBy={ReviewedBy}, Reason={Reason}",
                request.VerificationId, request.ReviewedBy, request.Reason);

            var verification = await _licenseRepository.GetByIdAsync(request.VerificationId, cancellationToken);
            if (verification == null)
                throw new InvalidOperationException($"审核记录不存在: {request.VerificationId}");

            verification.Reject(request.ReviewedBy, request.Reason);
            await _licenseRepository.UpdateAsync(verification, cancellationToken);

            _logger.LogInformation("营业执照审核拒绝: VerificationId={VerificationId}", request.VerificationId);
        }
    }
}
