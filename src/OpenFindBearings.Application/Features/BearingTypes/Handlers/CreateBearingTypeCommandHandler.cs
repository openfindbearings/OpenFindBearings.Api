using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.BearingTypes.Commands;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Features.BearingTypes.Handlers
{
    /// <summary>
    /// 创建轴承类型命令处理器
    /// </summary>
    public class CreateBearingTypeCommandHandler : IRequestHandler<CreateBearingTypeCommand, Guid>
    {
        private readonly IBearingTypeRepository _bearingTypeRepository;
        private readonly ILogger<CreateBearingTypeCommandHandler> _logger;

        public CreateBearingTypeCommandHandler(
            IBearingTypeRepository bearingTypeRepository,
            ILogger<CreateBearingTypeCommandHandler> logger)
        {
            _bearingTypeRepository = bearingTypeRepository;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateBearingTypeCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("创建轴承类型: Code={Code}, Name={Name}", request.Code, request.Name);

            // 检查代码是否已存在
            var existing = await _bearingTypeRepository.GetByCodeAsync(request.Code, cancellationToken);
            if (existing != null)
            {
                throw new InvalidOperationException($"轴承类型代码已存在: {request.Code}");
            }

            var bearingType = new BearingType(request.Code, request.Name, request.Description);
            await _bearingTypeRepository.AddAsync(bearingType, cancellationToken);

            _logger.LogInformation("轴承类型创建成功: Id={Id}, Code={Code}", bearingType.Id, bearingType.Code);

            return bearingType.Id;
        }
    }
}
