using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Interchanges.RejectInterchange
{
    /// <summary>平台驳回替代候选命令（v2.16.0）：记录理由，提交方可改后重提</summary>
    public record RejectInterchangeCommand : IRequest, ICommand
    {
        /// <summary>替代关系ID</summary>
        public Guid Id { get; init; }

        /// <summary>驳回理由（必填，提交方可见）</summary>
        public string Reason { get; init; } = string.Empty;
    }

    /// <summary>平台驳回替代候选处理器</summary>
    public class RejectInterchangeCommandHandler : IRequestHandler<RejectInterchangeCommand>
    {
        private readonly IBearingInterchangeRepository _interchangeRepository;
        private readonly ILogger<RejectInterchangeCommandHandler> _logger;

        public RejectInterchangeCommandHandler(
            IBearingInterchangeRepository interchangeRepository,
            ILogger<RejectInterchangeCommandHandler> logger)
        {
            _interchangeRepository = interchangeRepository;
            _logger = logger;
        }

        /// <summary>置为已驳回并记录理由</summary>
        public async Task Handle(RejectInterchangeCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                throw new InvalidOperationException("驳回理由不能为空");

            var entity = await _interchangeRepository.GetByIdAsync(request.Id, cancellationToken);
            if (entity == null)
                throw new InvalidOperationException($"替代关系不存在: {request.Id}");

            entity.Reject(request.Reason.Trim());
            await _interchangeRepository.UpdateAsync(entity, cancellationToken);
            _logger.LogInformation("平台驳回替代关系: Id={Id}", request.Id);
        }
    }
}