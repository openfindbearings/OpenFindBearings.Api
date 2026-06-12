using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.BearingTypes.DeleteBearingType
{
    /// <summary>
    /// 删除轴承类型命令处理器（软删除）
    /// </summary>
    public class DeleteBearingTypeCommandHandler : IRequestHandler<DeleteBearingTypeCommand>
    {
        private readonly IBearingTypeRepository _bearingTypeRepository;
        private readonly ILogger<DeleteBearingTypeCommandHandler> _logger;

        public DeleteBearingTypeCommandHandler(
            IBearingTypeRepository bearingTypeRepository,
            ILogger<DeleteBearingTypeCommandHandler> logger)
        {
            _bearingTypeRepository = bearingTypeRepository;
            _logger = logger;
        }

        public async Task Handle(DeleteBearingTypeCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始删除轴承类型: {BearingTypeId}", request.Id);

            var bearingType = await _bearingTypeRepository.GetByIdAsync(request.Id, cancellationToken);
            if (bearingType == null)
            {
                throw new InvalidOperationException($"轴承类型不存在: {request.Id}");
            }

            bearingType.Deactivate();
            await _bearingTypeRepository.UpdateAsync(bearingType, cancellationToken);

            _logger.LogInformation("轴承类型删除成功: {BearingTypeId}", request.Id);
        }
    }
}
