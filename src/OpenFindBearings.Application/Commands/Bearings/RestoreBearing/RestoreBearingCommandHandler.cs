using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Bearings.RestoreBearing
{
    /// <summary>
    /// 恢复已删除轴承命令处理器
    /// </summary>
    public class RestoreBearingCommandHandler : IRequestHandler<RestoreBearingCommand>
    {
        private readonly IBearingRepository _bearingRepository;
        private readonly ILogger<RestoreBearingCommandHandler> _logger;

        public RestoreBearingCommandHandler(
            IBearingRepository bearingRepository,
            ILogger<RestoreBearingCommandHandler> logger)
        {
            _bearingRepository = bearingRepository;
            _logger = logger;
        }

        public async Task Handle(RestoreBearingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始恢复轴承: {BearingId}", request.Id);

            var bearing = await _bearingRepository.GetByIdIgnoringFilterAsync(request.Id, cancellationToken);
            if (bearing == null)
            {
                throw new InvalidOperationException($"轴承不存在: {request.Id}");
            }

            bearing.Activate();
            await _bearingRepository.UpdateAsync(bearing, cancellationToken);

            _logger.LogInformation("轴承恢复成功: {BearingId}", request.Id);
        }
    }
}
