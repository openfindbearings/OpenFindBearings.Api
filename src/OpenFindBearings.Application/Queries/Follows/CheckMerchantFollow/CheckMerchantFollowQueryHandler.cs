using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Follows.CheckMerchantFollow
{
    /// <summary>
    /// 检查商家关注状态查询处理器
    /// </summary>
    public class CheckMerchantFollowQueryHandler : IRequestHandler<CheckMerchantFollowQuery, bool>
    {
        private readonly IUserMerchantFollowRepository _followRepository;
        private readonly ILogger<CheckMerchantFollowQueryHandler> _logger;

        public CheckMerchantFollowQueryHandler(
            IUserMerchantFollowRepository followRepository,
            ILogger<CheckMerchantFollowQueryHandler> logger)
        {
            _followRepository = followRepository;
            _logger = logger;
        }

        public async Task<bool> Handle(CheckMerchantFollowQuery request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("检查商家关注状态: UserId={UserId}, MerchantId={MerchantId}",
                request.UserId, request.MerchantId);

            return await _followRepository.ExistsAsync(request.UserId, request.MerchantId, cancellationToken);
        }
    }
}
