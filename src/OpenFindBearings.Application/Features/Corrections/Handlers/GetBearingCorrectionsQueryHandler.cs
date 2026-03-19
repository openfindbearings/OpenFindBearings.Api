using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Corrections.DTOs;
using OpenFindBearings.Application.Features.Corrections.Queries;
using OpenFindBearings.Domain.Common.Models;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Corrections.Handlers
{
    /// <summary>
    /// 获取轴承纠错列表查询处理器
    /// </summary>
    public class GetBearingCorrectionsQueryHandler : IRequestHandler<GetBearingCorrectionsQuery, PagedResult<CorrectionDto>>
    {
        private readonly ICorrectionRequestRepository _correctionRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetBearingCorrectionsQueryHandler> _logger;

        public GetBearingCorrectionsQueryHandler(
            ICorrectionRequestRepository correctionRepository,
            IUserRepository userRepository,
            ILogger<GetBearingCorrectionsQueryHandler> logger)
        {
            _correctionRepository = correctionRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<PagedResult<CorrectionDto>> Handle(GetBearingCorrectionsQuery request, CancellationToken cancellationToken)
        {
            var corrections = await _correctionRepository.GetByTargetAsync("Bearing", request.BearingId, cancellationToken);

            var totalCount = corrections.Count;
            var items = corrections
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(async c =>
                {
                    var submitter = await _userRepository.GetByIdAsync(c.SubmittedBy, cancellationToken);
                    var reviewer = c.ReviewedBy.HasValue
                        ? await _userRepository.GetByIdAsync(c.ReviewedBy.Value, cancellationToken)
                        : null;

                    return new CorrectionDto
                    {
                        Id = c.Id,
                        TargetType = c.TargetType,
                        TargetId = c.TargetId,
                        TargetDisplay = request.BearingId.ToString(),
                        FieldName = c.FieldName,
                        FieldDisplayName = c.GetFieldDisplayName(),
                        OriginalValue = c.OriginalValue,
                        SuggestedValue = c.SuggestedValue,
                        Reason = c.Reason,
                        SubmittedBy = c.SubmittedBy,
                        SubmitterName = submitter?.Nickname ?? "未知用户",
                        SubmittedAt = c.SubmittedAt,
                        Status = c.Status.ToString(),
                        ReviewerName = reviewer?.Nickname,
                        ReviewedAt = c.ReviewedAt,
                        ReviewComment = c.ReviewComment
                    };
                })
                .Select(t => t.Result)
                .ToList();

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
