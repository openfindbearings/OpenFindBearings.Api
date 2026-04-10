using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Corrections.GetPendingCorrections
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
            var items = corrections
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(c => c.ToDto())
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
