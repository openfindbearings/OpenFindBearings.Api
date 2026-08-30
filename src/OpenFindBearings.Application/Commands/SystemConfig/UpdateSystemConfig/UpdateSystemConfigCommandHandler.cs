using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.SystemConfig.UpdateSystemConfig
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

            // 改动说明：价格类配置的缓存失效不在此处执行。事务由 UnitOfWorkBehavior 在本 Handler
            //           返回之后才提交，若在 Handler 内失效，并发请求会在提交前重新加载旧值并缓存，
            //           失效被覆盖。失效动作已上移到 AdminEndpoints 的 mediator.Send 之后
            _logger.LogInformation("系统配置更新成功: Key={Key}", request.Key);
        }
    }
}
