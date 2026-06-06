using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Application.Commands.Sync.BatchCreateBearings
{
    /// <summary>
    /// 批量同步轴承命令处理器（重构版）
    /// 性能优化：消除逐条 MediatR.Send 导致的 50 次独立 SaveChanges，改为直接操作实体 + 外层 UnitOfWork 单次提交
    /// </summary>
    public class BatchCreateBearingsCommandHandler : IRequestHandler<BatchCreateBearingsCommand, BatchResult>
    {
        private readonly IBearingRepository _bearingRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly IBearingTypeRepository _bearingTypeRepository;
        private readonly ILogger<BatchCreateBearingsCommandHandler> _logger;

        public BatchCreateBearingsCommandHandler(
            IBearingRepository bearingRepository,
            IBrandRepository brandRepository,
            IBearingTypeRepository bearingTypeRepository,
            ILogger<BatchCreateBearingsCommandHandler> logger)
        {
            _bearingRepository = bearingRepository;
            _brandRepository = brandRepository;
            _bearingTypeRepository = bearingTypeRepository;
            _logger = logger;
        }

        public async Task<BatchResult> Handle(BatchCreateBearingsCommand request, CancellationToken ct)
        {
            _logger.LogInformation("批量同步轴承: {Count}, 模式: {Mode}", request.Bearings.Count, request.Mode);
            var result = new BatchResult();

            foreach (var dto in request.Bearings)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var brand = await _brandRepository.GetByCodeAsync(dto.BrandCode, ct);
                    if (brand == null)
                    {
                        result.AddFailed(dto.PartNumber, $"品牌不存在: {dto.BrandCode}");
                        continue;
                    }

                    var type = await _bearingTypeRepository.GetByCodeAsync(dto.TypeName, ct);
                    if (type == null)
                    {
                        result.AddFailed(dto.PartNumber, $"轴承类型不存在: {dto.TypeName}");
                        continue;
                    }

                    var existing = await _bearingRepository.GetByPartNumberAsync(dto.PartNumber, ct);

                    // DataSource 保护逻辑：非爬虫数据不可被覆盖
                    if (existing != null && existing.DataSource?.SourceType != DataSourceType.Crawler)
                    {
                        result.AddSkipped(dto.PartNumber, "非爬虫数据，跳过覆盖保护");
                        _logger.LogDebug("跳过覆盖: {PartNumber}, 来源: {Source}", dto.PartNumber, existing.DataSource?.SourceType);
                        continue;
                    }

                    if (request.Mode == SyncMode.Create && existing != null)
                    {
                        result.AddFailed(dto.PartNumber, "轴承型号已存在");
                        continue;
                    }

                    if (request.Mode == SyncMode.Update && existing == null)
                    {
                        result.AddFailed(dto.PartNumber, "轴承型号不存在");
                        continue;
                    }

                    if (existing == null)
                    {
                        var bearing = CreateBearing(dto, type, brand);
                        await _bearingRepository.AddAsync(bearing, ct);
                        result.AddSuccess(dto.PartNumber, "created", bearing.Id);
                    }
                    else
                    {
                        UpdateBearing(existing, dto);
                        await _bearingRepository.UpdateAsync(existing, ct);
                        result.AddSuccess(dto.PartNumber, "updated", existing.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "轴承同步失败: {PartNumber}", dto.PartNumber);
                    result.AddFailed(dto.PartNumber, ex.Message);
                }
            }

            _logger.LogInformation("批量同步轴承完成: 成功{Success}, 失败{Fail}",
                result.SuccessCount, result.FailCount);

            return result;
        }

        private static Bearing CreateBearing(SyncBearingDto dto, BearingType type, Brand brand)
        {
            var dimensions = new Dimensions(dto.InnerDiameter, dto.OuterDiameter, dto.Width);

            PerformanceParams? perf = null;
            if (dto.DynamicLoad.HasValue || dto.StaticLoad.HasValue || dto.LimitingSpeed.HasValue || dto.LimitingSpeedGrease.HasValue || dto.LimitingSpeedOil.HasValue)
                perf = new PerformanceParams(dto.DynamicLoad, dto.StaticLoad, dto.LimitingSpeed, dto.LimitingSpeedGrease, dto.LimitingSpeedOil);

            Bearing bearing;
            if (dto.IsStandard)
                bearing = new Bearing(dto.PartNumber, type.Id, type.Name, dimensions, brand.Id, perf, dto.Weight);
            else
                bearing = Bearing.CreateNonStandard(dto.PartNumber, type.Id, type.Name, dimensions, brand.Id, dto.StructureType, dto.SizeSeries, perf, dto.Weight);

            bearing.UpdateIdentification(dto.OldNumber, dto.CodeSource, dto.Trademark);
            bearing.UpdateTechnicalSpecs(dto.PrecisionGrade, dto.Material, dto.SealType, dto.CageType);
            bearing.UpdateDimensionDetails(dto.ChamferRmin, dto.ChamferRmax);
            bearing.UpdateDetails(dto.Description, dto.Weight);
            bearing.SetOrigin(dto.OriginCountry, dto.Category);
            bearing.SetImages(dto.Image3DUrl, dto.Image2DUrl);
            SetDataSource(bearing, dto);

            return bearing;
        }

        private static void UpdateBearing(Bearing bearing, SyncBearingDto dto)
        {
            if (dto.OldNumber != null || dto.CodeSource != null || dto.Trademark != null)
                bearing.UpdateIdentification(dto.OldNumber, dto.CodeSource, dto.Trademark);

            if (dto.Description != null || dto.Weight.HasValue)
                bearing.UpdateDetails(dto.Description, dto.Weight);

            if (dto.ChamferRmin.HasValue || dto.ChamferRmax.HasValue)
                bearing.UpdateDimensionDetails(dto.ChamferRmin, dto.ChamferRmax);

            if (dto.PrecisionGrade != null || dto.Material != null || dto.SealType != null || dto.CageType != null)
                bearing.UpdateTechnicalSpecs(dto.PrecisionGrade, dto.Material, dto.SealType, dto.CageType);

            if (dto.DynamicLoad.HasValue || dto.StaticLoad.HasValue || dto.LimitingSpeed.HasValue || dto.LimitingSpeedGrease.HasValue || dto.LimitingSpeedOil.HasValue)
            {
                var perf = new PerformanceParams(dto.DynamicLoad, dto.StaticLoad, dto.LimitingSpeed, dto.LimitingSpeedGrease, dto.LimitingSpeedOil);
                bearing.UpdatePerformance(perf);
            }

            if (dto.OriginCountry != null)
                bearing.SetOrigin(dto.OriginCountry, dto.Category);
            else if (dto.Category != default(BearingCategory))
                bearing.SetOrigin(bearing.OriginCountry, dto.Category);

            if (dto.Image3DUrl != null || dto.Image2DUrl != null)
                bearing.SetImages(dto.Image3DUrl, dto.Image2DUrl);
        }

        private static void SetDataSource(Bearing bearing, SyncBearingDto dto)
        {
            var sourceType = dto.DataSource ?? "manual";
            var importedBy = dto.SourceSite;

            if (sourceType.Equals("crawler", StringComparison.OrdinalIgnoreCase))
                bearing.SetDataSource(DataSource.FromCrawler(importedBy ?? "unknown"));
            else if (sourceType.Equals("api", StringComparison.OrdinalIgnoreCase))
                bearing.SetDataSource(DataSource.FromApi(importedBy ?? "ApiSync"));
            else if (sourceType.Equals("file", StringComparison.OrdinalIgnoreCase) || sourceType.Equals("fileimport", StringComparison.OrdinalIgnoreCase))
                bearing.SetDataSource(DataSource.FromFileImport(importedBy));
            else if (sourceType.Equals("seeddata", StringComparison.OrdinalIgnoreCase) || sourceType.Equals("seed", StringComparison.OrdinalIgnoreCase))
                bearing.SetDataSource(DataSource.FromSeedData());
            else
                bearing.SetDataSource(DataSource.FromManual(importedBy));
        }
    }
}
