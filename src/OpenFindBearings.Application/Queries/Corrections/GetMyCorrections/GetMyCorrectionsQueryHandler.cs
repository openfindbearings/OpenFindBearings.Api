using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Corrections.Queries
{
    /// <summary>
    /// 获取我提交的纠错列表查询处理器
    /// </summary>
    public class GetMyCorrectionsQueryHandler : IRequestHandler<GetMyCorrectionsQuery, PagedResult<CorrectionDto>>
    {
        private readonly ICorrectionRequestRepository _correctionRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetMyCorrectionsQueryHandler> _logger;

        public GetMyCorrectionsQueryHandler(
            ICorrectionRequestRepository correctionRepository,
            IBearingRepository bearingRepository,
            IMerchantRepository merchantRepository,
            IUserRepository userRepository,
            ILogger<GetMyCorrectionsQueryHandler> logger)
        {
            _correctionRepository = correctionRepository;
            _bearingRepository = bearingRepository;
            _merchantRepository = merchantRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<PagedResult<CorrectionDto>> Handle(
            GetMyCorrectionsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取用户提交的纠错列表: UserId={UserId}, Page={Page}, PageSize={PageSize}",
                request.UserId, request.Page, request.PageSize);

            var corrections = await _correctionRepository.GetByUserAsync(request.UserId, cancellationToken);

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
