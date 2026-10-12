using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Interchanges.ApproveInterchange
{
    /// <summary>平台审核通过替代候选命令（v2.16.0）：通过后公域可见</summary>
    public record ApproveInterchangeCommand : IRequest, ICommand
    {
        /// <summary>替代关系ID</summary>
        public Guid Id { get; init; }
    }

    /// <summary>平台审核通过替代候选处理器</summary>
    public class ApproveInterchangeCommandHandler : IRequestHandler<ApproveInterchangeCommand>
    {
        private readonly IBearingInterchangeRepository _interchangeRepository;
        private readonly ILogger<ApproveInterchangeCommandHandler> _logger;

        public ApproveInterchangeCommandHandler(
            IBearingInterchangeRepository interchangeRepository,
            ILogger<ApproveInterchangeCommandHandler> logger)
        {
            _interchangeRepository = interchangeRepository;
            _logger = logger;
        }

        /// <summary>置为已生效并清空驳回理由</summary>
        public async Task Handle(ApproveInterchangeCommand request, CancellationToken cancellationToken)
        {
            var entity = await _interchangeRepository.GetByIdAsync(request.Id, cancellationToken);
            if (entity == null)
                throw new InvalidOperationException($"替代关系不存在: {request.Id}");

            entity.Approve();
            await _interchangeRepository.UpdateAsync(entity, cancellationToken);
            _logger.LogInformation("平台审核通过替代关系: Id={Id}", request.Id);
        }
    }
}