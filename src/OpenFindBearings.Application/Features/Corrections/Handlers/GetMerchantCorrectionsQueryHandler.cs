using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Corrections.DTOs;
using OpenFindBearings.Application.Features.Corrections.Queries;
using OpenFindBearings.Domain.Common;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Corrections.Handlers
{
    /// <summary>
    /// 获取商家纠错列表查询处理器
    /// </summary>
    public class GetMerchantCorrectionsQueryHandler : IRequestHandler<GetMerchantCorrectionsQuery, PagedResult<CorrectionDto>>
    {
        private readonly ICorrectionRequestRepository _correctionRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetMerchantCorrectionsQueryHandler> _logger;

        public GetMerchantCorrectionsQueryHandler(
            ICorrectionRequestRepository correctionRepository,
            IUserRepository userRepository,
            ILogger<GetMerchantCorrectionsQueryHandler> logger)
        {
            _correctionRepository = correctionRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<PagedResult<CorrectionDto>> Handle(GetMerchantCorrectionsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取商家纠错列表: MerchantId={MerchantId}, Page={Page}, PageSize={PageSize}",
                request.MerchantId, request.Page, request.PageSize);

            var corrections = await _correctionRepository.GetByTargetAsync("Merchant", request.MerchantId, cancellationToken);

            var totalCount = corrections.Count;
            var items = new List<CorrectionDto>();

            foreach (var c in corrections.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize))
            {
                var submitter = await _userRepository.GetByIdAsync(c.SubmittedBy, cancellationToken);
                var reviewer = c.ReviewedBy.HasValue
                    ? await _userRepository.GetByIdAsync(c.ReviewedBy.Value, cancellationToken)
                    : null;

                items.Add(new CorrectionDto
                {
                    Id = c.Id,
                    TargetType = c.TargetType,
                    TargetId = c.TargetId,
                    TargetDisplay = request.MerchantId.ToString(),
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
