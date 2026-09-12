using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.ValueObjects;

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
                cancellationToken.ThrowIfCancellationRequested();
                var identifier = $"{dto.SourcePartNumber}({dto.SourceBrandCode}) -> {dto.TargetPartNumber}({dto.TargetBrandCode})";

                try
                {
                    // 本次写入来源类型（Manual/FileImport 透传，其余视为爬虫）
                    var incomingSourceType = dto.DataSource switch
                    {
                        "Manual" => DataSourceType.Manual,
                        "FileImport" => DataSourceType.FileImport,
                        _ => DataSourceType.Crawler
                    };

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
                            // 覆盖保护：人工维护的替代品不被爬虫同步覆盖
                            if (existing.DataSource != null
                                && existing.DataSource.SourceType != DataSourceType.Crawler)
                            {
                                result.AddSkipped(identifier, "人工维护数据，跳过爬虫覆盖保护");
                                continue;
                            }

                            existing.UpdateConfidence(dto.Confidence);
                            existing.UpdateRemarks(dto.Remarks);
                            // 修复 B4：本次为人工维护来源时同步标记 DataSource，
                            // 否则存量爬虫行被人工审核后仍无 Manual 标记，下一轮爬虫会再次覆盖（保护形同虚设）
                            if (incomingSourceType != DataSourceType.Crawler)
                            {
                                existing.SetDataSource(incomingSourceType == DataSourceType.FileImport
                                    ? DataSource.FromFileImport()
                                    : DataSource.FromManual());
                            }
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

                        // 覆盖保护：按本次来源类型标记数据来源，供后续爬虫同步判定是否可覆盖
                        interchange.SetDataSource(incomingSourceType switch
                        {
                            DataSourceType.Manual => DataSource.FromManual(),
                            DataSourceType.FileImport => DataSource.FromFileImport(),
                            _ => DataSource.FromCrawler(dto.Source ?? "cbia")
                        });

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
