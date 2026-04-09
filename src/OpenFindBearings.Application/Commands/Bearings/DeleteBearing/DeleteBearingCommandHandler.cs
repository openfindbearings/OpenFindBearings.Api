using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Bearings.DeleteBearing
{
    /// <summary>
    /// 删除轴承命令处理器
    /// </summary>
    public class DeleteBearingCommandHandler : IRequestHandler<DeleteBearingCommand>
    {
        private readonly IBearingRepository _bearingRepository;
        private readonly ILogger<DeleteBearingCommandHandler> _logger;

        public DeleteBearingCommandHandler(
            IBearingRepository bearingRepository,
            ILogger<DeleteBearingCommandHandler> logger)
        {
            _bearingRepository = bearingRepository;
            _logger = logger;
        }

        public async Task Handle(DeleteBearingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始删除轴承: {BearingId}", request.Id);

            var bearing = await _bearingRepository.GetByIdAsync(request.Id, cancellationToken);
            if (bearing == null)
            {
                throw new InvalidOperationException($"轴承不存在: {request.Id}");
            }

            bearing.Deactivate(); // 软删除
            await _bearingRepository.UpdateAsync(bearing, cancellationToken);

            _logger.LogInformation("轴承删除成功: {BearingId}", request.Id);
        }
    }
}
