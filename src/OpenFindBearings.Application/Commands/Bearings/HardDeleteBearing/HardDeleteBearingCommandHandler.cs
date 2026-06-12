using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Bearings.HardDeleteBearing
{
    /// <summary>
    /// 彻底删除轴承命令处理器（物理删除）
    /// </summary>
    public class HardDeleteBearingCommandHandler : IRequestHandler<HardDeleteBearingCommand>
    {
        private readonly IBearingRepository _bearingRepository;
        private readonly ILogger<HardDeleteBearingCommandHandler> _logger;

        public HardDeleteBearingCommandHandler(
            IBearingRepository bearingRepository,
            ILogger<HardDeleteBearingCommandHandler> logger)
        {
            _bearingRepository = bearingRepository;
            _logger = logger;
        }

        public async Task Handle(HardDeleteBearingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始彻底删除轴承: {BearingId}", request.Id);

            var bearing = await _bearingRepository.GetByIdIgnoringFilterAsync(request.Id, cancellationToken);
            if (bearing == null)
            {
                throw new InvalidOperationException($"轴承不存在: {request.Id}");
            }

            if (bearing.IsActive)
            {
                throw new InvalidOperationException("不能彻底删除激活状态的轴承，请先软删除");
            }

            await _bearingRepository.RemoveAsync(bearing, cancellationToken);

            _logger.LogInformation("轴承彻底删除成功: {BearingId}", request.Id);
        }
    }
}
