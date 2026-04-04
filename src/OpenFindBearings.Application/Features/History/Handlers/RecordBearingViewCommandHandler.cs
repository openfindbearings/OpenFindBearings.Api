using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.History.Commands;
using OpenFindBearings.Application.Interfaces;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.History.Handlers
{
    /// <summary>
    /// 记录轴承浏览历史命令处理器
    /// </summary>
    public class RecordBearingViewCommandHandler : IRequestHandler<RecordBearingViewCommand>
    {
        private readonly IUserBearingHistoryRepository _historyRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly IBearingViewStatsService _statsService;
        private readonly ILogger<RecordBearingViewCommandHandler> _logger;

        public RecordBearingViewCommandHandler(
            IUserBearingHistoryRepository historyRepository,
            IBearingRepository bearingRepository,
            IBearingViewStatsService statsService,
            ILogger<RecordBearingViewCommandHandler> logger)
        {
            _historyRepository = historyRepository;
            _bearingRepository = bearingRepository;
            _statsService = statsService;
            _logger = logger;
        }

        public async Task Handle(RecordBearingViewCommand request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("记录轴承浏览: UserId={UserId}, BearingId={BearingId}",
                request.UserId, request.BearingId);

            // 检查轴承是否存在
            var bearing = await _bearingRepository.GetByIdAsync(request.BearingId, cancellationToken);
            if (bearing == null)
            {
                _logger.LogWarning("尝试记录不存在的轴承: {BearingId}", request.BearingId);
                return;
            }

            // 记录浏览历史
            await _historyRepository.AddOrUpdateAsync(request.UserId, request.BearingId, cancellationToken);

            // 记录浏览次数（用于统计）
            await _statsService.RecordViewAsync(
                request.BearingId,
                request.UserId,
                null,
                DateTime.UtcNow,
                cancellationToken);

            _logger.LogDebug("轴承浏览记录成功: UserId={UserId}, BearingId={BearingId}",
                request.UserId, request.BearingId);
        }
    }
}
