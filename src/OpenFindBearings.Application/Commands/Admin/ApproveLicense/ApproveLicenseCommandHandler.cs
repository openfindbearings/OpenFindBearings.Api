using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Admin.ApproveLicense
{
    public class ApproveLicenseCommandHandler : IRequestHandler<ApproveLicenseCommand>
    {
        private readonly ILicenseVerificationRepository _licenseRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly ILogger<ApproveLicenseCommandHandler> _logger;

        public ApproveLicenseCommandHandler(
            ILicenseVerificationRepository licenseRepository,
            IMerchantRepository merchantRepository,
            IMerchantBearingRepository merchantBearingRepository,
            ILogger<ApproveLicenseCommandHandler> logger)
        {
            _licenseRepository = licenseRepository;
            _merchantRepository = merchantRepository;
            _merchantBearingRepository = merchantBearingRepository;
            _logger = logger;
        }

        public async Task Handle(ApproveLicenseCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("审核通过营业执照: VerificationId={VerificationId}, ReviewedBy={ReviewedBy}",
                request.VerificationId, request.ReviewedBy);

            var verification = await _licenseRepository.GetByIdAsync(request.VerificationId, cancellationToken);
            if (verification == null)
                throw new InvalidOperationException($"审核记录不存在: {request.VerificationId}");

            if (verification.Status != LicenseVerificationStatus.Pending)
                throw new InvalidOperationException($"营业执照审核记录已审核: {request.VerificationId}");

            verification.Approve(request.ReviewedBy, request.Comment);
            await _licenseRepository.UpdateAsync(verification, cancellationToken);

            // 认证商家
            var merchant = await _merchantRepository.GetByIdAsync(verification.MerchantId, cancellationToken);
            if (merchant != null && !merchant.IsVerified)
            {
                merchant.Verify(request.ReviewedBy.ToString());
                await _merchantRepository.UpdateAsync(merchant, cancellationToken);

                // 审核通过后清除该商户的爬虫关联数据
                await _merchantBearingRepository.DeleteByMerchantAndSourceAsync(
                    merchant.Id, DataSourceType.Crawler, cancellationToken);

                _logger.LogInformation("商家已认证, 已清除爬虫数据: MerchantId={MerchantId}", merchant.Id);
            }

            _logger.LogInformation("营业执照审核通过: VerificationId={VerificationId}", request.VerificationId);
        }
    }
}
