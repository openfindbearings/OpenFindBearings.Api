using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.History.Commands;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.History.Handlers
{
    /// <summary>
    /// 清空浏览历史命令处理器
    /// </summary>
    public class ClearHistoryCommandHandler : IRequestHandler<ClearHistoryCommand>
    {
        private readonly IUserBearingHistoryRepository _bearingHistoryRepository;
        private readonly IUserMerchantHistoryRepository _merchantHistoryRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<ClearHistoryCommandHandler> _logger;

        public ClearHistoryCommandHandler(
            IUserBearingHistoryRepository bearingHistoryRepository,
            IUserMerchantHistoryRepository merchantHistoryRepository,
            IUserRepository userRepository,
            ILogger<ClearHistoryCommandHandler> logger)
        {
            _bearingHistoryRepository = bearingHistoryRepository;
            _merchantHistoryRepository = merchantHistoryRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task Handle(ClearHistoryCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("清空用户浏览历史: UserId={AuthUserId}", request.UserId);

            var user = await _userRepository.GetByAuthUserIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("用户不存在: {AuthUserId}", request.UserId);
                return;
            }

            await _bearingHistoryRepository.ClearByUserIdAsync(user.Id, cancellationToken);
            await _merchantHistoryRepository.ClearByUserIdAsync(user.Id, cancellationToken);

            _logger.LogInformation("用户浏览历史已清空: UserId={UserId}", user.Id);
        }
    }
}
