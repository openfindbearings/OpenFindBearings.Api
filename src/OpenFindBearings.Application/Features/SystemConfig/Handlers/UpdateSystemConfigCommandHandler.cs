using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.SystemConfig.Commands;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.SystemConfig.Handlers
{
    /// <summary>
    /// 更新系统配置命令处理器
    /// </summary>
    public class UpdateSystemConfigCommandHandler : IRequestHandler<UpdateSystemConfigCommand>
    {
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly ILogger<UpdateSystemConfigCommandHandler> _logger;

        public UpdateSystemConfigCommandHandler(
            ISystemConfigRepository systemConfigRepository,
            ILogger<UpdateSystemConfigCommandHandler> logger)
        {
            _systemConfigRepository = systemConfigRepository;
            _logger = logger;
        }

        public async Task Handle(
            UpdateSystemConfigCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("更新系统配置: Key={Key}, Value={Value}, UpdatedBy={UpdatedBy}",
                request.Key, request.Value, request.UpdatedBy);

            var config = await _systemConfigRepository.GetByKeyAsync(request.Key, cancellationToken);
            if (config == null)
            {
                throw new InvalidOperationException($"配置不存在: {request.Key}");
            }

            config.UpdateValue(request.Value, request.UpdatedBy);
            await _systemConfigRepository.UpdateAsync(config, cancellationToken);

            _logger.LogInformation("系统配置更新成功: Key={Key}", request.Key);
        }
    }
}
