using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Admin.Commands;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Admin.Handlers
{
    public class ApproveLicenseCommandHandler : IRequestHandler<ApproveLicenseCommand>
    {
        private readonly ILicenseVerificationRepository _licenseRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<ApproveLicenseCommandHandler> _logger;

        public ApproveLicenseCommandHandler(
            ILicenseVerificationRepository licenseRepository,
            IMerchantRepository merchantRepository,
            ILogger<ApproveLicenseCommandHandler> logger)
        {
            _licenseRepository = licenseRepository;
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        public async Task Handle(ApproveLicenseCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("审核通过营业执照: VerificationId={VerificationId}, ReviewedBy={ReviewedBy}",
                request.VerificationId, request.ReviewedBy);

            var verification = await _licenseRepository.GetByIdAsync(request.VerificationId, cancellationToken);
            if (verification == null)
                throw new InvalidOperationException($"审核记录不存在: {request.VerificationId}");

            verification.Approve(request.ReviewedBy, request.Comment);
            await _licenseRepository.UpdateAsync(verification, cancellationToken);

            // 认证商家
            var merchant = await _merchantRepository.GetByIdAsync(verification.MerchantId, cancellationToken);
            if (merchant != null && !merchant.IsVerified)
            {
                merchant.Verify();
                await _merchantRepository.UpdateAsync(merchant, cancellationToken);
                _logger.LogInformation("商家已认证: MerchantId={MerchantId}", merchant.Id);
            }

            _logger.LogInformation("营业执照审核通过: VerificationId={VerificationId}", request.VerificationId);
        }
    }
}
