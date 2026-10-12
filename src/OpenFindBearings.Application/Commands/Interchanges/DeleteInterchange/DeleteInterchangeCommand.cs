using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Interchanges.DeleteInterchange
{
    /// <summary>平台删除替代关系命令（v2.16.0）：物理删除，唯一键释放</summary>
    public record DeleteInterchangeCommand : IRequest, ICommand
    {
        /// <summary>替代关系ID</summary>
        public Guid Id { get; init; }
    }

    /// <summary>平台删除替代关系处理器</summary>
    public class DeleteInterchangeCommandHandler : IRequestHandler<DeleteInterchangeCommand>
    {
        private readonly IBearingInterchangeRepository _interchangeRepository;
        private readonly ILogger<DeleteInterchangeCommandHandler> _logger;

        public DeleteInterchangeCommandHandler(
            IBearingInterchangeRepository interchangeRepository,
            ILogger<DeleteInterchangeCommandHandler> logger)
        {
            _interchangeRepository = interchangeRepository;
            _logger = logger;
        }

        /// <summary>加载后物理删除</summary>
        public async Task Handle(DeleteInterchangeCommand request, CancellationToken cancellationToken)
        {
            var entity = await _interchangeRepository.GetByIdAsync(request.Id, cancellationToken);
            if (entity == null)
                throw new InvalidOperationException($"替代关系不存在: {request.Id}");

            await _interchangeRepository.DeleteAsync(request.Id, cancellationToken);
            _logger.LogInformation("平台删除替代关系: Id={Id}", request.Id);
        }
    }
}