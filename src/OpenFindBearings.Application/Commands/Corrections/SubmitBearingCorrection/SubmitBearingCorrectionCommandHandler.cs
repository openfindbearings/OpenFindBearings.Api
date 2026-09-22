using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Commands.Corrections.Commands;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Corrections.SubmitBearingCorrection
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

            // 改动说明（v2.14.0）：去重守卫——同一用户对同一轴承同一字段已有待审纠错时幂等拒绝，
            //   防连点/反复提交刷审核队列（纠错量小，GetByTarget 后内存过滤即可，不加专用仓储方法）
            var pending = await _correctionRepository.GetByTargetAsync("Bearing", request.BearingId, cancellationToken);
            if (pending.Any(c => c.Status == CorrectionStatus.Pending
                && c.SubmittedBy == request.SubmittedBy
                && string.Equals(c.FieldName, request.FieldName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("该字段的纠错已在审核中，请耐心等待处理结果");
            }

            // 获取原始值
            string? originalValue = request.FieldName.ToLower() switch
            {
                "partnumber" => bearing.PartNumber,
                "oldnumber" => bearing.OldNumber,
                "description" => bearing.Description,
                "innerdiameter" => bearing.Dimensions.InnerDiameter.ToString(),
                "outerdiameter" => bearing.Dimensions.OuterDiameter.ToString(),
                "width" => bearing.Dimensions.Width.ToString(),
                "weight" => bearing.Weight?.ToString(),
                "precisiongrade" => bearing.PrecisionGrade,
                "material" => bearing.Material,
                "sealtype" => bearing.SealType,
                "cagetype" => bearing.CageType,
                "dynamicload" => bearing.Performance?.DynamicLoad?.ToString(),
                "staticload" => bearing.Performance?.StaticLoad?.ToString(),
                "limitingspeed" => bearing.Performance?.LimitingSpeed?.ToString(),
                "limitingspeedgrease" => bearing.Performance?.LimitingSpeedGrease?.ToString(),
                "limitingspeedoil" => bearing.Performance?.LimitingSpeedOil?.ToString(),
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
