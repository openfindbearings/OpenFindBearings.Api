using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.History.Commands;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.History.Handlers
{
    /// <summary>
    /// 记录商家浏览历史命令处理器
    /// </summary>
    public class RecordMerchantViewCommandHandler : IRequestHandler<RecordMerchantViewCommand>
    {
        private readonly IUserMerchantHistoryRepository _historyRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<RecordMerchantViewCommandHandler> _logger;

        public RecordMerchantViewCommandHandler(
            IUserMerchantHistoryRepository historyRepository,
            IUserRepository userRepository,
            IMerchantRepository merchantRepository,
            ILogger<RecordMerchantViewCommandHandler> logger)
        {
            _historyRepository = historyRepository;
            _userRepository = userRepository;
            _merchantRepository = merchantRepository;
            _logger = logger;
        }

        public async Task Handle(RecordMerchantViewCommand request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("记录商家浏览: UserId={AuthUserId}, MerchantId={MerchantId}",
                request.UserId, request.MerchantId);

            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken);
            if (merchant == null)
            {
                _logger.LogWarning("尝试记录不存在的商家: {MerchantId}", request.MerchantId);
                return;
            }

            var user = await _userRepository.GetByAuthUserIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("用户不存在，无法记录历史: {AuthUserId}", request.UserId);
                return;
            }

            await _historyRepository.AddOrUpdateAsync(user.Id, request.MerchantId, cancellationToken);

            _logger.LogDebug("商家浏览记录成功: UserId={UserId}, MerchantId={MerchantId}",
                user.Id, request.MerchantId);
        }
    }
}
