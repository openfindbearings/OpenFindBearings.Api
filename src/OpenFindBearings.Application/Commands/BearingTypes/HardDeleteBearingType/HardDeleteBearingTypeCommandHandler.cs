using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.BearingTypes.HardDeleteBearingType
{
    /// <summary>
    /// 彻底删除轴承类型命令处理器（物理删除）
    /// </summary>
    public class HardDeleteBearingTypeCommandHandler : IRequestHandler<HardDeleteBearingTypeCommand>
    {
        private readonly IBearingTypeRepository _bearingTypeRepository;
        private readonly ILogger<HardDeleteBearingTypeCommandHandler> _logger;

        public HardDeleteBearingTypeCommandHandler(
            IBearingTypeRepository bearingTypeRepository,
            ILogger<HardDeleteBearingTypeCommandHandler> logger)
        {
            _bearingTypeRepository = bearingTypeRepository;
            _logger = logger;
        }

        public async Task Handle(HardDeleteBearingTypeCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始彻底删除轴承类型: {BearingTypeId}", request.Id);

            var bearingType = await _bearingTypeRepository.GetByIdIgnoringFilterAsync(request.Id, cancellationToken);
            if (bearingType == null)
            {
                throw new InvalidOperationException($"轴承类型不存在: {request.Id}");
            }

            if (bearingType.IsActive)
            {
                throw new InvalidOperationException("不能彻底删除激活状态的轴承类型，请先软删除");
            }

            await _bearingTypeRepository.RemoveAsync(bearingType, cancellationToken);

            _logger.LogInformation("轴承类型彻底删除成功: {BearingTypeId}", request.Id);
        }
    }
}
