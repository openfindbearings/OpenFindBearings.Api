using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Follows.Queries;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Follows.Handlers
{
    /// <summary>
    /// 检查商家关注状态查询处理器
    /// </summary>
    public class CheckMerchantFollowQueryHandler : IRequestHandler<CheckMerchantFollowQuery, bool>
    {
        private readonly IUserFollowRepository _followRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<CheckMerchantFollowQueryHandler> _logger;

        public CheckMerchantFollowQueryHandler(
            IUserFollowRepository followRepository,
            IUserRepository userRepository,
            ILogger<CheckMerchantFollowQueryHandler> logger)
        {
            _followRepository = followRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<bool> Handle(CheckMerchantFollowQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByAuthUserIdAsync(request.AuthUserId, cancellationToken);
            if (user == null)
            {
                return false;
            }

            return await _followRepository.ExistsAsync(user.Id, request.MerchantId, cancellationToken);
        }
    }
}
