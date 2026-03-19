using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Corrections.Commands;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Corrections.Handlers
{
    /// <summary>
    /// 提交商家纠错命令处理器
    /// </summary>
    public class SubmitMerchantCorrectionCommandHandler : IRequestHandler<SubmitMerchantCorrectionCommand, Guid>
    {
        private readonly ICorrectionRequestRepository _correctionRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<SubmitMerchantCorrectionCommandHandler> _logger;

        public SubmitMerchantCorrectionCommandHandler(
            ICorrectionRequestRepository correctionRepository,
            IMerchantRepository merchantRepository,
            ILogger<SubmitMerchantCorrectionCommandHandler> logger)
        {
            _correctionRepository = correctionRepository;
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        public async Task<Guid> Handle(SubmitMerchantCorrectionCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("提交商家纠错: MerchantId={MerchantId}, Field={FieldName}, UserId={UserId}",
                request.MerchantId, request.FieldName, request.SubmittedBy);

            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken);
            if (merchant == null)
            {
                throw new InvalidOperationException($"商家不存在: {request.MerchantId}");
            }

            // 获取原始值
            string? originalValue = request.FieldName.ToLower() switch
            {
                "name" => merchant.Name,
                "companyname" => merchant.CompanyName,
                "contactperson" => merchant.Contact?.ContactPerson,
                "phone" => merchant.Contact?.Phone,
                "mobile" => merchant.Contact?.Mobile,
                "email" => merchant.Contact?.Email,
                "address" => merchant.Contact?.Address,
                "description" => merchant.Description,
                "businessscope" => merchant.BusinessScope,
                _ => null
            };

            var correction = CorrectionRequest.ForMerchant(
                merchantId: request.MerchantId,
                fieldName: request.FieldName,
                suggestedValue: request.SuggestedValue,
                submittedBy: request.SubmittedBy,
                originalValue: originalValue,
                reason: request.Reason
            );

            await _correctionRepository.AddAsync(correction, cancellationToken);

            _logger.LogInformation("商家纠错提交成功: CorrectionId={CorrectionId}", correction.Id);

            return correction.Id;
        }
    }
}
