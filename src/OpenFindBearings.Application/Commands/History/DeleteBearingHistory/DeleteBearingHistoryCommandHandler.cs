using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.History.DeleteBearingHistory
{
    /// <summary>
    /// 删除单条轴承浏览历史处理器：先按用户维度查出历史行再删（仓储 DeleteAsync 按行 Id，
    /// 直接传行 Id 会越权，故此处以 userId+bearingId 收敛归属）
    /// </summary>
    public class DeleteBearingHistoryCommandHandler : IRequestHandler<DeleteBearingHistoryCommand>
    {
        private readonly IUserBearingHistoryRepository _bearingHistoryRepository;
        private readonly ILogger<DeleteBearingHistoryCommandHandler> _logger;

        public DeleteBearingHistoryCommandHandler(
            IUserBearingHistoryRepository bearingHistoryRepository,
            ILogger<DeleteBearingHistoryCommandHandler> logger)
        {
            _bearingHistoryRepository = bearingHistoryRepository;
            _logger = logger;
        }

        public async Task Handle(DeleteBearingHistoryCommand request, CancellationToken cancellationToken)
        {
            var history = await _bearingHistoryRepository.GetAsync(request.UserId, request.BearingId, cancellationToken);
            if (history == null)
            {
                _logger.LogInformation("删除轴承浏览历史：记录不存在 UserId={UserId} BearingId={BearingId}",
                    request.UserId, request.BearingId);
                return;
            }

            await _bearingHistoryRepository.DeleteAsync(history.Id, cancellationToken);
            _logger.LogInformation("删除轴承浏览历史成功: UserId={UserId} BearingId={BearingId}",
                request.UserId, request.BearingId);
        }
    }
}
