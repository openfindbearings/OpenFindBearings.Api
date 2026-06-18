using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.BearingTypes.RestoreBearingType
{
    /// <summary>
    /// 恢复已删除轴承类型命令处理器
    /// </summary>
    public class RestoreBearingTypeCommandHandler : IRequestHandler<RestoreBearingTypeCommand>
    {
        private readonly IBearingTypeRepository _bearingTypeRepository;
        private readonly ILogger<RestoreBearingTypeCommandHandler> _logger;

        public RestoreBearingTypeCommandHandler(
            IBearingTypeRepository bearingTypeRepository,
            ILogger<RestoreBearingTypeCommandHandler> logger)
        {
            _bearingTypeRepository = bearingTypeRepository;
            _logger = logger;
        }

        public async Task Handle(RestoreBearingTypeCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始恢复轴承类型: {BearingTypeId}", request.Id);

            var bearingType = await _bearingTypeRepository.GetByIdIgnoringFilterAsync(request.Id, cancellationToken);
            if (bearingType == null)
            {
                throw new InvalidOperationException($"轴承类型不存在: {request.Id}");
            }

            bearingType.Activate();
            await _bearingTypeRepository.UpdateAsync(bearingType, cancellationToken);

            _logger.LogInformation("轴承类型恢复成功: {BearingTypeId}", request.Id);
        }
    }
}
