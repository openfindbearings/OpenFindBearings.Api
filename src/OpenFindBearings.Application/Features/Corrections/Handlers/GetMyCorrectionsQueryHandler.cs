using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Corrections.DTOs;
using OpenFindBearings.Application.Features.Corrections.Queries;
using OpenFindBearings.Domain.Common;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Corrections.Handlers
{
    /// <summary>
    /// 获取我提交的纠错列表查询处理器
    /// </summary>
    public class GetMyCorrectionsQueryHandler : IRequestHandler<GetMyCorrectionsQuery, PagedResult<CorrectionDto>>
    {
        private readonly ICorrectionRequestRepository _correctionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<GetMyCorrectionsQueryHandler> _logger;

        public GetMyCorrectionsQueryHandler(
            ICorrectionRequestRepository correctionRepository,
            IUserRepository userRepository,
            IBearingRepository bearingRepository,
            IMerchantRepository merchantRepository,
            ILogger<GetMyCorrectionsQueryHandler> logger)
        {
            _correctionRepository = correctionRepository;
            _userRepository = userRepository;
            _bearingRepository = bearingRepository;
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        public async Task<PagedResult<CorrectionDto>> Handle(GetMyCorrectionsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取用户提交的纠错列表: UserId={AuthUserId}, Page={Page}, PageSize={PageSize}",
                request.AuthUserId, request.Page, request.PageSize);

            var user = await _userRepository.GetByAuthUserIdAsync(request.AuthUserId, cancellationToken);
            if (user == null)
            {
                return new PagedResult<CorrectionDto>();
            }

            var corrections = await _correctionRepository.GetByUserAsync(user.Id, cancellationToken);

            var totalCount = corrections.Count;
            var items = new List<CorrectionDto>();

            foreach (var c in corrections.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize))
            {
                string targetDisplay = string.Empty;

                if (c.TargetType == "Bearing")
                {
                    var bearing = await _bearingRepository.GetByIdAsync(c.TargetId, cancellationToken);
                    targetDisplay = bearing != null ? $"{bearing.PartNumber} - {bearing.Name}" : string.Empty;
                }
                else if (c.TargetType == "Merchant")
                {
                    var merchant = await _merchantRepository.GetByIdAsync(c.TargetId, cancellationToken);
                    targetDisplay = merchant?.Name ?? string.Empty;
                }

                var reviewer = c.ReviewedBy.HasValue
                    ? await _userRepository.GetByIdAsync(c.ReviewedBy.Value, cancellationToken)
                    : null;

                items.Add(new CorrectionDto
                {
                    Id = c.Id,
                    TargetType = c.TargetType,
                    TargetId = c.TargetId,
                    TargetDisplay = targetDisplay,
                    FieldName = c.FieldName,
                    FieldDisplayName = c.GetFieldDisplayName(),
                    OriginalValue = c.OriginalValue,
                    SuggestedValue = c.SuggestedValue,
                    Reason = c.Reason,
                    SubmittedBy = c.SubmittedBy,
                    SubmitterName = user.Nickname ?? "我",
                    SubmittedAt = c.SubmittedAt,
                    Status = c.Status.ToString(),
                    ReviewerName = reviewer?.Nickname,
                    ReviewedAt = c.ReviewedAt,
                    ReviewComment = c.ReviewComment
                });
            }

            return new PagedResult<CorrectionDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
