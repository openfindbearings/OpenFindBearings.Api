using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Common.Interfaces;
using OpenFindBearings.Application.Features.History.Commands;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.History.Handlers
{
    /// <summary>
    /// 记录轴承浏览历史命令处理器
    /// </summary>
    public class RecordBearingViewCommandHandler : IRequestHandler<RecordBearingViewCommand>
    {
        private readonly IUserBearingHistoryRepository _historyRepository;
        private readonly IUserRepository _userRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly IBearingViewStatsService _statsService;
        private readonly ILogger<RecordBearingViewCommandHandler> _logger;

        public RecordBearingViewCommandHandler(
            IUserBearingHistoryRepository historyRepository,
            IUserRepository userRepository,
            IBearingRepository bearingRepository,
            IBearingViewStatsService statsService,
            ILogger<RecordBearingViewCommandHandler> logger)
        {
            _historyRepository = historyRepository;
            _userRepository = userRepository;
            _bearingRepository = bearingRepository;
            _statsService = statsService;
            _logger = logger;
        }

        public async Task Handle(RecordBearingViewCommand request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("记录轴承浏览: UserId={AuthUserId}, BearingId={BearingId}",
                request.UserId, request.BearingId);

            // 检查轴承是否存在
            var bearing = await _bearingRepository.GetByIdAsync(request.BearingId, cancellationToken);
            if (bearing == null)
            {
                _logger.LogWarning("尝试记录不存在的轴承: {BearingId}", request.BearingId);
                return;
            }

            // 获取业务用户ID
            var user = await _userRepository.GetByAuthUserIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("用户不存在，无法记录历史: {AuthUserId}", request.UserId);
                return;
            }

            // 记录浏览历史
            await _historyRepository.AddOrUpdateAsync(user.Id, request.BearingId, cancellationToken);

            // 记录浏览次数（用于统计）
            await _statsService.RecordViewAsync(
                request.BearingId,
                user.Id,
                null,
                DateTime.UtcNow,
                cancellationToken);

            _logger.LogDebug("轴承浏览记录成功: UserId={UserId}, BearingId={BearingId}",
                user.Id, request.BearingId);
        }
    }
}
