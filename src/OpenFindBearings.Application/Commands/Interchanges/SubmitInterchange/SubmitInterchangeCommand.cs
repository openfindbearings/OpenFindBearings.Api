using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Application.Commands.Interchanges.SubmitInterchange
{
    /// <summary>商家提交替代候选命令（v2.16.0）：置待审核，待平台审核生效</summary>
    public record SubmitInterchangeCommand : IRequest<Guid>, ICommand
    {
        /// <summary>源轴承ID（被替代方）</summary>
        public Guid SourceBearingId { get; init; }

        /// <summary>目标轴承ID（本商户可交付的替代方）</summary>
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

        /// <summary>提交商户ID（由端点从当前商户上下文注入，不信任客户端入参）</summary>
        public Guid MerchantId { get; init; }
    }

    /// <summary>商家提交替代候选处理器</summary>
    public class SubmitInterchangeCommandHandler : IRequestHandler<SubmitInterchangeCommand, Guid>
    {
        private readonly IBearingRepository _bearingRepository;
        private readonly IBearingInterchangeRepository _interchangeRepository;
        private readonly ILogger<SubmitInterchangeCommandHandler> _logger;

        public SubmitInterchangeCommandHandler(
            IBearingRepository bearingRepository,
            IBearingInterchangeRepository interchangeRepository,
            ILogger<SubmitInterchangeCommandHandler> logger)
        {
            _bearingRepository = bearingRepository;
            _interchangeRepository = interchangeRepository;
            _logger = logger;
        }

        /// <summary>校验两轴承存在且未重复后创建待审候选，来源标记为用户提交并记录提出商户</summary>
        public async Task<Guid> Handle(SubmitInterchangeCommand request, CancellationToken cancellationToken)
        {
            if (request.MerchantId == Guid.Empty)
                throw new InvalidOperationException("未找到当前商户上下文");

            if (request.SourceBearingId == request.TargetBearingId)
                throw new InvalidOperationException("源轴承与目标轴承不能相同");

            var source = await _bearingRepository.GetByIdAsync(request.SourceBearingId, cancellationToken);
            if (source == null)
                throw new InvalidOperationException("源轴承不存在");

            var target = await _bearingRepository.GetByIdAsync(request.TargetBearingId, cancellationToken);
            if (target == null)
                throw new InvalidOperationException("目标轴承不存在");

            if (await _interchangeRepository.ExistsAsync(request.SourceBearingId, request.TargetBearingId, cancellationToken))
                throw new InvalidOperationException("该替代关系已存在或已提交，请在“我的替代提交”中查看");

            var entity = new BearingInterchange(
                request.SourceBearingId,
                request.TargetBearingId,
                request.InterchangeType,
                request.Confidence,
                request.Source,
                request.Remarks,
                request.IsBidirectional);

            entity.SubmitByMerchant(request.MerchantId);
            entity.SetDataSource(DataSource.FromUserSubmitted($"Merchant:{request.MerchantId}"));

            await _interchangeRepository.AddAsync(entity, cancellationToken);
            _logger.LogInformation("商家提交替代候选: {Source} -> {Target}, Merchant={MerchantId}",
                source.PartNumber, target.PartNumber, request.MerchantId);
            return entity.Id;
        }
    }
}