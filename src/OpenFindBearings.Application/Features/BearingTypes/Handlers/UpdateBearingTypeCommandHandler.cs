using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.BearingTypes.Commands;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.BearingTypes.Handlers
{
    /// <summary>
    /// 更新轴承类型命令处理器
    /// </summary>
    public class UpdateBearingTypeCommandHandler : IRequestHandler<UpdateBearingTypeCommand>
    {
        private readonly IBearingTypeRepository _bearingTypeRepository;
        private readonly ILogger<UpdateBearingTypeCommandHandler> _logger;

        public UpdateBearingTypeCommandHandler(
            IBearingTypeRepository bearingTypeRepository,
            ILogger<UpdateBearingTypeCommandHandler> logger)
        {
            _bearingTypeRepository = bearingTypeRepository;
            _logger = logger;
        }

        public async Task Handle(UpdateBearingTypeCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("更新轴承类型: Id={Id}", request.Id);

            var bearingType = await _bearingTypeRepository.GetByIdAsync(request.Id, cancellationToken);
            if (bearingType == null)
            {
                throw new InvalidOperationException($"轴承类型不存在: {request.Id}");
            }

            // 更新名称和描述
            bearingType.Update(request.Name, request.Description);

            await _bearingTypeRepository.UpdateAsync(bearingType, cancellationToken);

            _logger.LogInformation("轴承类型更新成功: Id={Id}", bearingType.Id);
        }
    }
}
