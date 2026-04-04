using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Corrections.Commands;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Application.Features.Corrections.Handlers
{
    /// <summary>
    /// 审核通过纠错命令处理器
    /// </summary>
    public class ApproveCorrectionCommandHandler : IRequestHandler<ApproveCorrectionCommand>
    {
        private readonly ICorrectionRequestRepository _correctionRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<ApproveCorrectionCommandHandler> _logger;

        public ApproveCorrectionCommandHandler(
            ICorrectionRequestRepository correctionRepository,
            IBearingRepository bearingRepository,
            IMerchantRepository merchantRepository,
            ILogger<ApproveCorrectionCommandHandler> logger)
        {
            _correctionRepository = correctionRepository;
            _bearingRepository = bearingRepository;
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        public async Task Handle(ApproveCorrectionCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("审核通过纠错: CorrectionId={CorrectionId}, Reviewer={ReviewerId}",
                request.CorrectionId, request.ReviewedBy);

            var correction = await _correctionRepository.GetByIdAsync(request.CorrectionId, cancellationToken);
            if (correction == null)
            {
                throw new InvalidOperationException($"纠错不存在: {request.CorrectionId}");
            }

            if (correction.TargetType == "Bearing")
            {
                await ApplyBearingCorrection(correction, cancellationToken);
            }
            else if (correction.TargetType == "Merchant")
            {
                await ApplyMerchantCorrection(correction, cancellationToken);
            }

            correction.Approve(request.ReviewedBy, request.Comment);
            await _correctionRepository.UpdateAsync(correction, cancellationToken);

            _logger.LogInformation("纠错审核通过成功: CorrectionId={CorrectionId}", correction.Id);
        }

        private async Task ApplyBearingCorrection(Domain.Entities.CorrectionRequest correction, CancellationToken cancellationToken)
        {
            var bearing = await _bearingRepository.GetByIdAsync(correction.TargetId, cancellationToken);
            if (bearing == null) return;

            switch (correction.FieldName.ToLower())
            {
                case "name":
                    // bearing.UpdateName(correction.SuggestedValue);
                    break;
                case "description":
                    bearing.UpdateDetails(correction.SuggestedValue, bearing.Weight);
                    break;
                case "precisiongrade":
                case "material":
                case "sealtype":
                case "cagetype":
                    bearing.UpdateTechnicalSpecs(
                        correction.FieldName.ToLower() == "precisiongrade" ? correction.SuggestedValue : bearing.PrecisionGrade,
                        correction.FieldName.ToLower() == "material" ? correction.SuggestedValue : bearing.Material,
                        correction.FieldName.ToLower() == "sealtype" ? correction.SuggestedValue : bearing.SealType,
                        correction.FieldName.ToLower() == "cagetype" ? correction.SuggestedValue : bearing.CageType
                    );
                    break;
            }

            await _bearingRepository.UpdateAsync(bearing, cancellationToken);
        }

        private async Task ApplyMerchantCorrection(Domain.Entities.CorrectionRequest correction, CancellationToken cancellationToken)
        {
            var merchant = await _merchantRepository.GetByIdAsync(correction.TargetId, cancellationToken);
            if (merchant == null) return;

            switch (correction.FieldName.ToLower())
            {
                case "name":
                    merchant.UpdateName(correction.SuggestedValue);
                    break;
                case "companyname":
                    // ✅ 修改：需要传递所有6个参数
                    merchant.UpdateBasicInfo(
                        companyName: correction.SuggestedValue,
                        unifiedSocialCreditCode: merchant.UnifiedSocialCreditCode,
                        description: merchant.Description,
                        businessScope: merchant.BusinessScope,
                        logoUrl: merchant.LogoUrl,
                        website: merchant.Website
                    );
                    break;
                case "description":
                    merchant.UpdateBasicInfo(
                        companyName: merchant.CompanyName,
                        unifiedSocialCreditCode: merchant.UnifiedSocialCreditCode,
                        description: correction.SuggestedValue,
                        businessScope: merchant.BusinessScope,
                        logoUrl: merchant.LogoUrl,
                        website: merchant.Website
                    );
                    break;
                case "businessscope":
                    merchant.UpdateBasicInfo(
                        companyName: merchant.CompanyName,
                        unifiedSocialCreditCode: merchant.UnifiedSocialCreditCode,
                        description: merchant.Description,
                        businessScope: correction.SuggestedValue,
                        logoUrl: merchant.LogoUrl,
                        website: merchant.Website
                    );
                    break;
                case "contactperson":
                case "phone":
                case "mobile":
                case "email":
                case "address":
                    var currentContact = merchant.Contact ?? new ContactInfo();
                    var newContact = new ContactInfo(
                        contactPerson: correction.FieldName.ToLower() == "contactperson" ? correction.SuggestedValue : currentContact.ContactPerson,
                        phone: correction.FieldName.ToLower() == "phone" ? correction.SuggestedValue : currentContact.Phone,
                        mobile: correction.FieldName.ToLower() == "mobile" ? correction.SuggestedValue : currentContact.Mobile,
                        email: correction.FieldName.ToLower() == "email" ? correction.SuggestedValue : currentContact.Email,
                        address: correction.FieldName.ToLower() == "address" ? correction.SuggestedValue : currentContact.Address
                    );
                    merchant.UpdateContact(newContact);
                    break;
            }

            await _merchantRepository.UpdateAsync(merchant, cancellationToken);
        }
    }
}
