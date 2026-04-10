using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Corrections.GetMerchantCorrections
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
