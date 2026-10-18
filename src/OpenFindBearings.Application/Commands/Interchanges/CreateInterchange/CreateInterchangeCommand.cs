using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Application.Commands.Interchanges.CreateInterchange
{
    /// <summary>平台新建替代关系命令（v2.16.0 替代品治理）：平台自建直接生效</summary>
    public record CreateInterchangeCommand : IRequest<Guid>, ICommand
    {
        /// <summary>源轴承ID（被替代方）</summary>
        public Guid SourceBearingId { get; init; }

        /// <summary>目标轴承ID（替代方）</summary>
        public Guid TargetBearingId { get; init; }

        /// <summary>替代类型（exact/conditional/functional）</summary>
        public string? InterchangeType { get; init; } = "exact";

        /// <summary>可信度 0-100</summary>
        public int Confidence { get; init; } = 80;

        /// <summary>来源说明</summary>
        public string? Source { get; init; }

        /// <summary>备注</summary>
        public string? Remarks { get; init; }

        /// <summary>是否双向替代</summary>
        public bool IsBidirectional { get; init; } = true;
    }

    /// <summary>平台新建替代关系处理器</summary>
    public class CreateInterchangeCommandHandler : IRequestHandler<CreateInterchangeCommand, Guid>
    {
        private readonly IBearingRepository _bearingRepository;
        private readonly IBearingInterchangeRepository _interchangeRepository;
        private readonly ILogger<CreateInterchangeCommandHandler> _logger;

        public CreateInterchangeCommandHandler(
            IBearingRepository bearingRepository,
            IBearingInterchangeRepository interchangeRepository,
            ILogger<CreateInterchangeCommandHandler> logger)
        {
            _bearingRepository = bearingRepository;
            _interchangeRepository = interchangeRepository;
            _logger = logger;
        }

        /// <summary>校验两轴承存在且未重复后创建，来源标记为人工维护、状态直接生效</summary>
        public async Task<Guid> Handle(CreateInterchangeCommand request, CancellationToken cancellationToken)
        {
            if (request.SourceBearingId == request.TargetBearingId)
                throw new InvalidOperationException("源轴承与目标轴承不能相同");

            var source = await _bearingRepository.GetByIdAsync(request.SourceBearingId, cancellationToken);
            if (source == null)
                throw new InvalidOperationException("源轴承不存在");

            var target = await _bearingRepository.GetByIdAsync(request.TargetBearingId, cancellationToken);
            if (target == null)
                throw new InvalidOperationException("目标轴承不存在");

            if (await _interchangeRepository.ExistsAsync(request.SourceBearingId, request.TargetBearingId, cancellationToken))
                throw new InvalidOperationException("该替代关系已存在");

            var entity = new BearingInterchange(
                request.SourceBearingId,
                request.TargetBearingId,
                request.InterchangeType,
                request.Confidence,
                request.Source,
                request.Remarks,
                request.IsBidirectional);

            // 改动说明（v2.17.0 替代品字典化）：平台新建即生效，不再需要 MarkApproved（审核状态机已取消）
            entity.SetDataSource(DataSource.FromManual());

            await _interchangeRepository.AddAsync(entity, cancellationToken);
            _logger.LogInformation("平台新建替代关系: {Source} -> {Target}", source.PartNumber, target.PartNumber);
            return entity.Id;
        }
    }
}