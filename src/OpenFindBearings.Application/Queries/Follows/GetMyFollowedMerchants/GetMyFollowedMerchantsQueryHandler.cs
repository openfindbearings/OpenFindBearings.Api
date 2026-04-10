using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Follows.GetMyFollowedMerchants
{
    /// <summary>
    /// 获取我关注的商家列表查询处理器
    /// </summary>
    public class GetMyFollowedMerchantsQueryHandler : IRequestHandler<GetMyFollowedMerchantsQuery, PagedResult<FollowedMerchantDto>>
    {
        private readonly IUserMerchantFollowRepository _followRepository;
        private readonly ILogger<GetMyFollowedMerchantsQueryHandler> _logger;

        public GetMyFollowedMerchantsQueryHandler(
            IUserMerchantFollowRepository followRepository,
            ILogger<GetMyFollowedMerchantsQueryHandler> logger)
        {
            _followRepository = followRepository;
            _logger = logger;
        }

        public async Task<PagedResult<FollowedMerchantDto>> Handle(
            GetMyFollowedMerchantsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取用户关注商家列表: UserId={UserId}, Page={Page}, PageSize={PageSize}",
                request.UserId, request.Page, request.PageSize);

            var follows = await _followRepository.GetByUserIdAsync(
                request.UserId,
                request.Page,
                request.PageSize,
                cancellationToken);

            var totalCount = await _followRepository.CountByUserIdAsync(request.UserId, cancellationToken);

            var items = follows.Select(f => f.ToDto()).ToList();

            return new PagedResult<FollowedMerchantDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
