using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Corrections.GetCorrectionDetail
{
    /// <summary>
    /// 获取单个纠错详情查询处理器
    /// </summary>
    public class GetCorrectionDetailQueryHandler : IRequestHandler<GetCorrectionDetailQuery, CorrectionDto?>
    {
        private readonly ICorrectionRequestRepository _correctionRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetCorrectionDetailQueryHandler> _logger;

        public GetCorrectionDetailQueryHandler(
            ICorrectionRequestRepository correctionRepository,
            IBearingRepository bearingRepository,
            IMerchantRepository merchantRepository,
            IUserRepository userRepository,
            ILogger<GetCorrectionDetailQueryHandler> logger)
        {
            _correctionRepository = correctionRepository;
            _bearingRepository = bearingRepository;
            _merchantRepository = merchantRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<CorrectionDto?> Handle(GetCorrectionDetailQuery request, CancellationToken cancellationToken)
        {
            var correction = await _correctionRepository.GetByIdAsync(request.Id, cancellationToken);
            if (correction == null)
                return null;

            var submitter = await _userRepository.GetByIdAsync(correction.SubmittedBy, cancellationToken);
            var reviewer = correction.ReviewedBy.HasValue
                ? await _userRepository.GetByIdAsync(correction.ReviewedBy.Value, cancellationToken)
                : null;

            string targetDisplay = string.Empty;
            if (correction.TargetType == "Bearing")
            {
                var bearing = await _bearingRepository.GetByIdAsync(correction.TargetId, cancellationToken);
                targetDisplay = bearing != null ? $"{bearing.CurrentCode} - {bearing.Name}" : string.Empty;
            }
            else if (correction.TargetType == "Merchant")
            {
                var merchant = await _merchantRepository.GetByIdAsync(correction.TargetId, cancellationToken);
                targetDisplay = merchant?.Name ?? string.Empty;
            }

            return new CorrectionDto
            {
                Id = correction.Id,
                TargetType = correction.TargetType,
                TargetId = correction.TargetId,
                TargetDisplay = targetDisplay,
                FieldName = correction.FieldName,
                FieldDisplayName = correction.GetFieldDisplayName(),
                OriginalValue = correction.OriginalValue,
                SuggestedValue = correction.SuggestedValue,
                Reason = correction.Reason,
                SubmittedBy = correction.SubmittedBy,
                SubmitterName = submitter?.Nickname ?? "未知用户",
                SubmittedAt = correction.SubmittedAt,
                Status = correction.Status.ToString(),
                ReviewerName = reviewer?.Nickname,
                ReviewedAt = correction.ReviewedAt,
                ReviewComment = correction.ReviewComment
            };
        }
    }
}
