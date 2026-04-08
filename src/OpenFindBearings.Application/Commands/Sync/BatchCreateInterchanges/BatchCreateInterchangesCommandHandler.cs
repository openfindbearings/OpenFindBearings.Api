using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Sync.BatchCreateInterchanges
{
    /// <summary>
    /// 批量创建替代品关系命令处理器
    /// </summary>
    public class BatchCreateInterchangesCommandHandler : IRequestHandler<BatchCreateInterchangesCommand, BatchResult>
    {
        private readonly IBearingRepository _bearingRepository;
        private readonly IBearingInterchangeRepository _interchangeRepository;
        private readonly ILogger<BatchCreateInterchangesCommandHandler> _logger;

        public BatchCreateInterchangesCommandHandler(
            IBearingRepository bearingRepository,
            IBearingInterchangeRepository interchangeRepository,
            ILogger<BatchCreateInterchangesCommandHandler> logger)
        {
            _bearingRepository = bearingRepository;
            _interchangeRepository = interchangeRepository;
            _logger = logger;
        }

        public async Task<BatchResult> Handle(BatchCreateInterchangesCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始批量创建替代品关系，数量: {Count}", request.Interchanges.Count);

            var result = new BatchResult();

            foreach (var dto in request.Interchanges)
            {
                var identifier = $"{dto.SourcePartNumber}({dto.SourceBrandCode}) -> {dto.TargetPartNumber}({dto.TargetBrandCode})";

                try
                {
                    // 查找源轴承
                    var sourceBearing = await FindBearing(dto.SourcePartNumber, dto.SourceBrandCode, cancellationToken);
                    if (sourceBearing == null)
                    {
                        result.AddFailed(identifier, $"源轴承不存在: {dto.SourcePartNumber} ({dto.SourceBrandCode})");
                        continue;
                    }

                    // 查找目标轴承
                    var targetBearing = await FindBearing(dto.TargetPartNumber, dto.TargetBrandCode, cancellationToken);
                    if (targetBearing == null)
                    {
                        result.AddFailed(identifier, $"目标轴承不存在: {dto.TargetPartNumber} ({dto.TargetBrandCode})");
                        continue;
                    }

                    // 检查是否已存在
                    var exists = await _interchangeRepository.ExistsAsync(sourceBearing.Id, targetBearing.Id, cancellationToken);

                    if (exists)
                    {
                        // 更新现有关系
                        var existing = (await _interchangeRepository.GetBySourceBearingAsync(sourceBearing.Id, cancellationToken))
                            .FirstOrDefault(i => i.TargetBearingId == targetBearing.Id);

                        if (existing != null)
                        {
                            existing.UpdateConfidence(dto.Confidence);
                            existing.UpdateRemarks(dto.Remarks);
                            await _interchangeRepository.UpdateAsync(existing, cancellationToken);
                            result.AddSuccess(identifier, "updated", existing.Id);
                        }
                    }
                    else
                    {
                        // 创建新关系
                        var interchange = new BearingInterchange(
                            sourceBearing.Id,
                            targetBearing.Id,
                            dto.InterchangeType,
                            dto.Confidence,
                            dto.Source,
                            dto.Remarks,
                            dto.IsBidirectional
                        );

                        await _interchangeRepository.AddAsync(interchange, cancellationToken);
                        result.AddSuccess(identifier, "created", interchange.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "批量创建替代品关系失败: {Identifier}", identifier);
                    result.AddFailed(identifier, ex.Message);
                }
            }

            return result;
        }

        private async Task<Bearing?> FindBearing(string partNumber, string brandCode, CancellationToken cancellationToken)
        {
            var bearing = await _bearingRepository.GetByPartNumberAsync(partNumber, cancellationToken);

            // 如果指定了品牌代码，验证品牌
            if (bearing != null && !string.IsNullOrEmpty(brandCode))
            {
                if (bearing.Brand?.Code != brandCode)
                {
                    return null;
                }
            }

            return bearing;
        }
    }
}
