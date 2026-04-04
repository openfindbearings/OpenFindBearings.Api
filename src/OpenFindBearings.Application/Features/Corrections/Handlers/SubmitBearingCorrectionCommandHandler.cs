using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Corrections.Commands;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Corrections.Handlers
{
    /// <summary>
    /// 提交轴承纠错命令处理器
    /// </summary>
    public class SubmitBearingCorrectionCommandHandler : IRequestHandler<SubmitBearingCorrectionCommand, Guid>
    {
        private readonly ICorrectionRequestRepository _correctionRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly ILogger<SubmitBearingCorrectionCommandHandler> _logger;

        public SubmitBearingCorrectionCommandHandler(
            ICorrectionRequestRepository correctionRepository,
            IBearingRepository bearingRepository,
            ILogger<SubmitBearingCorrectionCommandHandler> logger)
        {
            _correctionRepository = correctionRepository;
            _bearingRepository = bearingRepository;
            _logger = logger;
        }

        public async Task<Guid> Handle(SubmitBearingCorrectionCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("提交轴承纠错: BearingId={BearingId}, Field={FieldName}, UserId={UserId}",
                request.BearingId, request.FieldName, request.SubmittedBy);

            // 验证轴承是否存在
            var bearing = await _bearingRepository.GetByIdAsync(request.BearingId, cancellationToken);
            if (bearing == null)
            {
                throw new InvalidOperationException($"轴承不存在: {request.BearingId}");
            }

            // 获取原始值
            string? originalValue = request.FieldName.ToLower() switch
            {
                "partnumber" => bearing.CurrentCode,
                "name" => bearing.Name,
                "description" => bearing.Description,
                "innerdiameter" => bearing.Dimensions.InnerDiameter.ToString(),
                "outerdiameter" => bearing.Dimensions.OuterDiameter.ToString(),
                "width" => bearing.Dimensions.Width.ToString(),
                "weight" => bearing.Weight?.ToString(),
                "precisiongrade" => bearing.PrecisionGrade,
                "material" => bearing.Material,
                "sealtype" => bearing.SealType,
                "cagetype" => bearing.CageType,
                "dynamicloadrating" => bearing.Performance?.DynamicLoadRating?.ToString(),
                "staticloadrating" => bearing.Performance?.StaticLoadRating?.ToString(),
                "limitingspeed" => bearing.Performance?.LimitingSpeed?.ToString(),
                _ => null
            };

            // 创建纠错请求
            var correction = CorrectionRequest.ForBearing(
                bearingId: request.BearingId,
                fieldName: request.FieldName,
                suggestedValue: request.SuggestedValue,
                submittedBy: request.SubmittedBy,
                originalValue: originalValue,
                reason: request.Reason
            );

            await _correctionRepository.AddAsync(correction, cancellationToken);

            _logger.LogInformation("轴承纠错提交成功: CorrectionId={CorrectionId}", correction.Id);

            return correction.Id;
        }
    }
}
