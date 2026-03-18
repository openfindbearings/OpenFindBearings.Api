using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Corrections.Commands;
using OpenFindBearings.Domain.Interfaces;
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
        private readonly IUserRepository _userRepository;
        private readonly ILogger<ApproveCorrectionCommandHandler> _logger;

        public ApproveCorrectionCommandHandler(
            ICorrectionRequestRepository correctionRepository,
            IBearingRepository bearingRepository,
            IMerchantRepository merchantRepository,
            IUserRepository userRepository,
            ILogger<ApproveCorrectionCommandHandler> logger)
        {
            _correctionRepository = correctionRepository;
            _bearingRepository = bearingRepository;
            _merchantRepository = merchantRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task Handle(ApproveCorrectionCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("审核通过纠错: CorrectionId={CorrectionId}, Reviewer={AuthUserId}",
                request.CorrectionId, request.ReviewedBy);

            var correction = await _correctionRepository.GetByIdAsync(request.CorrectionId, cancellationToken);
            if (correction == null)
            {
                throw new InvalidOperationException($"纠错不存在: {request.CorrectionId}");
            }

            var reviewer = await _userRepository.GetByAuthUserIdAsync(request.ReviewedBy, cancellationToken);
            if (reviewer == null)
            {
                throw new InvalidOperationException($"审核人不存在: {request.ReviewedBy}");
            }

            // 更新对应实体的字段
            if (correction.TargetType == "Bearing")
            {
                await ApplyBearingCorrection(correction, cancellationToken);
            }
            else if (correction.TargetType == "Merchant")
            {
                await ApplyMerchantCorrection(correction, cancellationToken);
            }

            correction.Approve(reviewer.Id, request.Comment);
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

            // 根据字段名更新商家信息
            switch (correction.FieldName.ToLower())
            {
                case "name":
                    // 需要 Merchant 实体添加 UpdateName 方法
                    // merchant.UpdateName(correction.SuggestedValue);
                    break;
                case "companyname":
                    merchant.UpdateBasicInfo(
                        correction.SuggestedValue,
                        merchant.Description,
                        merchant.BusinessScope
                    );
                    break;
                case "description":
                    merchant.UpdateBasicInfo(
                        merchant.CompanyName,
                        correction.SuggestedValue,
                        merchant.BusinessScope
                    );
                    break;
                case "businessscope":
                    merchant.UpdateBasicInfo(
                        merchant.CompanyName,
                        merchant.Description,
                        correction.SuggestedValue
                    );
                    break;
                case "contactperson":
                case "phone":
                case "mobile":
                case "email":
                case "address":
                    // 更新联系方式
                    var currentContact = merchant.Contact ?? new ContactInfo();
                    var newContact = new ContactInfo(
                        correction.FieldName.ToLower() == "contactperson" ? correction.SuggestedValue : currentContact.ContactPerson,
                        correction.FieldName.ToLower() == "phone" ? correction.SuggestedValue : currentContact.Phone,
                        correction.FieldName.ToLower() == "mobile" ? correction.SuggestedValue : currentContact.Mobile,
                        correction.FieldName.ToLower() == "email" ? correction.SuggestedValue : currentContact.Email,
                        correction.FieldName.ToLower() == "address" ? correction.SuggestedValue : currentContact.Address
                    );
                    merchant.UpdateContact(newContact);
                    break;
            }

            await _merchantRepository.UpdateAsync(merchant, cancellationToken);
        }
    }
}
