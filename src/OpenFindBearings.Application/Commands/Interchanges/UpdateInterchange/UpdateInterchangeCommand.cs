using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Interchanges.UpdateInterchange
{
    /// <summary>平台编辑替代关系命令（v2.16.0）：更新类型/可信度/来源/备注/双向</summary>
    public record UpdateInterchangeCommand : IRequest, ICommand
    {
        /// <summary>替代关系ID</summary>
        public Guid Id { get; init; }

        /// <summary>替代类型（exact/conditional/functional）</summary>
        public string? InterchangeType { get; init; }

        /// <summary>可信度 0-100</summary>
        public int Confidence { get; init; } = 80;

        /// <summary>来源说明</summary>
        public string? Source { get; init; }

        /// <summary>备注</summary>
        public string? Remarks { get; init; }

        /// <summary>是否双向替代</summary>
        public bool IsBidirectional { get; init; } = true;
    }

    /// <summary>平台编辑替代关系处理器</summary>
    public class UpdateInterchangeCommandHandler : IRequestHandler<UpdateInterchangeCommand>
    {
        private readonly IBearingInterchangeRepository _interchangeRepository;
        private readonly ILogger<UpdateInterchangeCommandHandler> _logger;

        public UpdateInterchangeCommandHandler(
            IBearingInterchangeRepository interchangeRepository,
            ILogger<UpdateInterchangeCommandHandler> logger)
        {
            _interchangeRepository = interchangeRepository;
            _logger = logger;
        }

        /// <summary>加载后更新明细字段</summary>
        public async Task Handle(UpdateInterchangeCommand request, CancellationToken cancellationToken)
        {
            var entity = await _interchangeRepository.GetByIdAsync(request.Id, cancellationToken);
            if (entity == null)
                throw new InvalidOperationException($"替代关系不存在: {request.Id}");

            entity.UpdateDetails(request.InterchangeType, request.Confidence, request.Source, request.Remarks, request.IsBidirectional);
            await _interchangeRepository.UpdateAsync(entity, cancellationToken);
            _logger.LogInformation("平台编辑替代关系: Id={Id}", request.Id);
        }
    }
}