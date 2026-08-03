using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Application.Commands.Sync.BatchCreateBearingTypes
{
    /// <summary>
    /// 批量创建轴承类型命令处理器
    /// </summary>
    public class BatchCreateBearingTypesCommandHandler : IRequestHandler<BatchCreateBearingTypesCommand, BatchResult>
    {
        private readonly IBearingTypeRepository _bearingTypeRepository;
        private readonly ILogger<BatchCreateBearingTypesCommandHandler> _logger;

        public BatchCreateBearingTypesCommandHandler(
            IBearingTypeRepository bearingTypeRepository,
            ILogger<BatchCreateBearingTypesCommandHandler> logger)
        {
            _bearingTypeRepository = bearingTypeRepository;
            _logger = logger;
        }

        public async Task<BatchResult> Handle(BatchCreateBearingTypesCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始批量创建轴承类型，数量: {Count}, 模式: {Mode}",
                request.BearingTypes.Count, request.Mode);

            var result = new BatchResult();

            foreach (var typeDto in request.BearingTypes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    // 检查类型是否已存在
                    var existingType = await _bearingTypeRepository.GetByCodeAsync(typeDto.Code, cancellationToken);

                    // DataSource 保护逻辑：非爬虫数据不可被覆盖
                    if (existingType != null && existingType.DataSource?.SourceType != DataSourceType.Crawler)
                    {
                        result.AddSkipped(typeDto.Code, "非爬虫数据，跳过覆盖保护");
                        _logger.LogDebug("跳过覆盖: {Code}, 来源: {Source}", typeDto.Code, existingType.DataSource?.SourceType);
                        continue;
                    }

                    if (existingType != null && request.Mode == SyncMode.Create)
                    {
                        result.AddFailed(typeDto.Code, $"轴承类型已存在: {typeDto.Code}");
                        continue;
                    }

                    if (existingType == null && request.Mode == SyncMode.Update)
                    {
                        result.AddFailed(typeDto.Code, $"轴承类型不存在: {typeDto.Code}");
                        continue;
                    }

                    if (existingType == null)
                    {
                        // 创建新类型
                        var bearingType = new BearingType(
                            typeDto.Code,
                            typeDto.Name,
                            typeDto.Description);

                        SetDataSource(bearingType, typeDto);
                        await _bearingTypeRepository.AddAsync(bearingType, cancellationToken);
                        result.AddSuccess(typeDto.Code, "created", bearingType.Id);
                    }
                    else if (request.Mode == SyncMode.Update || request.Mode == SyncMode.Upsert)
                    {
                        // 更新现有类型
                        existingType.Update(typeDto.Name, typeDto.Description);

                        await _bearingTypeRepository.UpdateAsync(existingType, cancellationToken);
                        result.AddSuccess(typeDto.Code, "updated", existingType.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "批量创建轴承类型失败: {Code}", typeDto.Code);
                    result.AddFailed(typeDto.Code, ex.Message);
                }
            }

            _logger.LogInformation("批量创建轴承类型完成，成功: {SuccessCount}, 失败: {FailCount}",
                result.SuccessCount, result.FailCount);

            return result;
        }

        private static void SetDataSource(BearingType bearingType, SyncBearingTypeDto dto)
        {
            var sourceType = dto.DataSource ?? "manual";
            var importedBy = dto.SourceSite;

            if (sourceType.Equals("crawler", StringComparison.OrdinalIgnoreCase))
                bearingType.SetDataSource(DataSource.FromCrawler(importedBy ?? "unknown"));
            else if (sourceType.Equals("api", StringComparison.OrdinalIgnoreCase))
                bearingType.SetDataSource(DataSource.FromApi(importedBy ?? "ApiSync"));
            else if (sourceType.Equals("file", StringComparison.OrdinalIgnoreCase) || sourceType.Equals("fileimport", StringComparison.OrdinalIgnoreCase))
                bearingType.SetDataSource(DataSource.FromFileImport(importedBy));
            else if (sourceType.Equals("seeddata", StringComparison.OrdinalIgnoreCase) || sourceType.Equals("seed", StringComparison.OrdinalIgnoreCase))
                bearingType.SetDataSource(DataSource.FromSeedData());
            else
                bearingType.SetDataSource(DataSource.FromManual(importedBy));
        }
    }
}
