using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Corrections.Commands;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Corrections.Handlers
{
    /// <summary>
    /// 提交轴承纠错命令处理器
    /// </summary>
    public class SubmitBearingCorrectionCommandHandler : IRequestHandler<SubmitBearingCorrectionCommand, Guid>
    {
        private readonly ICorrectionRequestRepository _correctionRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<SubmitBearingCorrectionCommandHandler> _logger;

        public SubmitBearingCorrectionCommandHandler(
            ICorrectionRequestRepository correctionRepository,
            IBearingRepository bearingRepository,
            IUserRepository userRepository,
            ILogger<SubmitBearingCorrectionCommandHandler> logger)
        {
            _correctionRepository = correctionRepository;
            _bearingRepository = bearingRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<Guid> Handle(SubmitBearingCorrectionCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("提交轴承纠错: BearingId={BearingId}, Field={FieldName}, User={AuthUserId}",
                request.BearingId, request.FieldName, request.SubmittedBy);

            // 验证轴承是否存在
            var bearing = await _bearingRepository.GetByIdAsync(request.BearingId, cancellationToken);
            if (bearing == null)
            {
                throw new InvalidOperationException($"轴承不存在: {request.BearingId}");
            }

            // 获取用户ID
            var user = await _userRepository.GetByAuthUserIdAsync(request.SubmittedBy, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException($"用户不存在: {request.SubmittedBy}");
            }

            // 获取原始值
            string? originalValue = request.FieldName.ToLower() switch
            {
                "partnumber" => bearing.PartNumber,
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
            var correction = new CorrectionRequest(
                targetType: "Bearing",
                targetId: request.BearingId,
                fieldName: request.FieldName,
                suggestedValue: request.SuggestedValue,
                submittedBy: user.Id,
                originalValue: originalValue,
                reason: request.Reason
            );

            await _correctionRepository.AddAsync(correction, cancellationToken);

            _logger.LogInformation("轴承纠错提交成功: CorrectionId={CorrectionId}", correction.Id);

            return correction.Id;
        }
    }
}
