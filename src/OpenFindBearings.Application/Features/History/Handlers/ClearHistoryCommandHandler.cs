using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.History.Commands;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.History.Handlers
{
    /// <summary>
    /// 清空浏览历史命令处理器
    /// </summary>
    public class ClearHistoryCommandHandler : IRequestHandler<ClearHistoryCommand>
    {
        private readonly IUserBearingHistoryRepository _bearingHistoryRepository;
        private readonly IUserMerchantHistoryRepository _merchantHistoryRepository;
        private readonly ILogger<ClearHistoryCommandHandler> _logger;

        public ClearHistoryCommandHandler(
            IUserBearingHistoryRepository bearingHistoryRepository,
            IUserMerchantHistoryRepository merchantHistoryRepository,
            ILogger<ClearHistoryCommandHandler> logger)
        {
            _bearingHistoryRepository = bearingHistoryRepository;
            _merchantHistoryRepository = merchantHistoryRepository;
            _logger = logger;
        }

        public async Task Handle(ClearHistoryCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("清空用户浏览历史: UserId={UserId}", request.UserId);

            await _bearingHistoryRepository.ClearByUserIdAsync(request.UserId, cancellationToken);
            await _merchantHistoryRepository.ClearByUserIdAsync(request.UserId, cancellationToken);

            _logger.LogInformation("用户浏览历史已清空: UserId={UserId}", request.UserId);
        }
    }
}
