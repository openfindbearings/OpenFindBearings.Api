using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Corrections.DTOs;
using OpenFindBearings.Application.Features.Corrections.Queries;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.Corrections.Handlers
{
    /// <summary>
    /// 获取纠错列表查询处理器
    /// </summary>
    public class GetCorrectionsQueryHandler : IRequestHandler<GetCorrectionsQuery, PagedResult<CorrectionDto>>
    {
        private readonly ICorrectionRequestRepository _correctionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<GetCorrectionsQueryHandler> _logger;

        public GetCorrectionsQueryHandler(
            ICorrectionRequestRepository correctionRepository,
            IUserRepository userRepository,
            IBearingRepository bearingRepository,
            IMerchantRepository merchantRepository,
            ILogger<GetCorrectionsQueryHandler> logger)
        {
            _correctionRepository = correctionRepository;
            _userRepository = userRepository;
            _bearingRepository = bearingRepository;
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        public async Task<PagedResult<CorrectionDto>> Handle(
            GetCorrectionsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取纠错列表: Page={Page}, PageSize={PageSize}", request.Page, request.PageSize);

            // 获取所有纠错（这里需要仓储支持条件查询）
            // 简化处理：先获取所有，再过滤
            var allCorrections = await _correctionRepository.GetAllAsync(cancellationToken);

            // 应用过滤条件
            var filtered = allCorrections.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.TargetType))
            {
                filtered = filtered.Where(c => c.TargetType == request.TargetType);
            }

            if (request.TargetId.HasValue)
            {
                filtered = filtered.Where(c => c.TargetId == request.TargetId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<CorrectionStatus>(request.Status, out var status))
            {
                filtered = filtered.Where(c => c.Status == status);
            }

            var totalCount = filtered.Count();
            var items = filtered
                .OrderByDescending(c => c.SubmittedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(async c =>
                {
                    var submitter = await _userRepository.GetByIdAsync(c.SubmittedBy, cancellationToken);
                    var reviewer = c.ReviewedBy.HasValue
                        ? await _userRepository.GetByIdAsync(c.ReviewedBy.Value, cancellationToken)
                        : null;

                    string targetDisplay = string.Empty;
                    if (c.TargetType == "Bearing")
                    {
                        var bearing = await _bearingRepository.GetByIdAsync(c.TargetId, cancellationToken);
                        targetDisplay = bearing != null ? $"{bearing.CurrentCode} - {bearing.Name}" : string.Empty;
                    }
                    else if (c.TargetType == "Merchant")
                    {
                        var merchant = await _merchantRepository.GetByIdAsync(c.TargetId, cancellationToken);
                        targetDisplay = merchant?.Name ?? string.Empty;
                    }

                    return new CorrectionDto
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
