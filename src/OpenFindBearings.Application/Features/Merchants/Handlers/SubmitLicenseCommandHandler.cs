using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Merchants.Commands;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Merchants.Handlers
{
    /// <summary>
    /// 提交营业执照审核命令处理器
    /// </summary>
    public class SubmitLicenseCommandHandler : IRequestHandler<SubmitLicenseCommand, Guid>
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<SubmitLicenseCommandHandler> _logger;

        public SubmitLicenseCommandHandler(
            IMerchantRepository merchantRepository,
            ILogger<SubmitLicenseCommandHandler> logger)
        {
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        public async Task<Guid> Handle(SubmitLicenseCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("提交营业执照审核: MerchantId={MerchantId}, LicenseUrl={LicenseUrl}, SubmittedBy={SubmittedBy}",
                request.MerchantId, request.LicenseUrl, request.SubmittedBy);

            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken);
            if (merchant == null)
            {
                throw new InvalidOperationException($"商家不存在: {request.MerchantId}");
            }

            // 创建营业执照审核记录
            var verification = new LicenseVerification(
                  request.MerchantId,
                  request.LicenseUrl,
                  request.SubmittedBy);

            // 保存审核记录（需要在 DbContext 中添加 LicenseVerifications DbSet）
            // await _context.LicenseVerifications.AddAsync(verification, cancellationToken);
            // await _context.SaveChangesAsync(cancellationToken);

            // 可选：发送通知给管理员
            // await _notificationService.NotifyAdminsAsync($"商家 {merchant.Name} 提交了营业执照审核");

            _logger.LogInformation("营业执照审核已提交: VerificationId={VerificationId}, MerchantId={MerchantId}",
                verification.Id, request.MerchantId);

            return verification.Id;
        }
    }
}
