using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Corrections.DTOs;
using OpenFindBearings.Application.Features.Corrections.Queries;
using OpenFindBearings.Domain.Common.Models;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Corrections.Handlers
{
    /// <summary>
    /// 获取待审核纠错列表查询处理器（管理员用）
    /// </summary>
    public class GetPendingCorrectionsQueryHandler : IRequestHandler<GetPendingCorrectionsQuery, PagedResult<CorrectionDto>>
    {
        private readonly ICorrectionRequestRepository _correctionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<GetPendingCorrectionsQueryHandler> _logger;

        public GetPendingCorrectionsQueryHandler(
            ICorrectionRequestRepository correctionRepository,
            IUserRepository userRepository,
            IBearingRepository bearingRepository,
            IMerchantRepository merchantRepository,
            ILogger<GetPendingCorrectionsQueryHandler> logger)
        {
            _correctionRepository = correctionRepository;
            _userRepository = userRepository;
            _bearingRepository = bearingRepository;
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        public async Task<PagedResult<CorrectionDto>> Handle(GetPendingCorrectionsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取待审核纠错列表: Page={Page}, PageSize={PageSize}",
                request.Page, request.PageSize);

            var corrections = await _correctionRepository.GetPendingAsync(cancellationToken);

            var totalCount = corrections.Count;
            var items = new List<CorrectionDto>();

            foreach (var c in corrections.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize))
            {
                var submitter = await _userRepository.GetByIdAsync(c.SubmittedBy, cancellationToken);

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
                    SubmitterName = submitter?.Nickname ?? "未知用户",
                    SubmittedAt = c.SubmittedAt,
                    Status = c.Status.ToString(),
                    ReviewerName = null,
                    ReviewedAt = null,
                    ReviewComment = null
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
