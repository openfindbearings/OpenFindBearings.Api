using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Commands.Bearings.CreateBearing;
using OpenFindBearings.Application.Commands.Bearings.UpdateBearing;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Sync.BatchCreateBearings
{
    /// <summary>
    /// 批量创建轴承命令处理器
    /// </summary>
    public class BatchCreateBearingsCommandHandler : IRequestHandler<BatchCreateBearingsCommand, BatchResult>
    {
        private readonly IMediator _mediator;
        private readonly IBearingRepository _bearingRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly IBearingTypeRepository _bearingTypeRepository;
        private readonly ILogger<BatchCreateBearingsCommandHandler> _logger;

        public BatchCreateBearingsCommandHandler(
            IMediator mediator,
            IBearingRepository bearingRepository,
            IBrandRepository brandRepository,
            IBearingTypeRepository bearingTypeRepository,
            ILogger<BatchCreateBearingsCommandHandler> logger)
        {
            _mediator = mediator;
            _bearingRepository = bearingRepository;
            _brandRepository = brandRepository;
            _bearingTypeRepository = bearingTypeRepository;
            _logger = logger;
        }

        public async Task<BatchResult> Handle(BatchCreateBearingsCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始批量创建轴承，数量: {Count}, 模式: {Mode}",
                request.Bearings.Count, request.Mode);

            var result = new BatchResult();

            foreach (var bearingDto in request.Bearings)
            {
                try
                {
                    // 验证品牌是否存在
                    var brand = await _brandRepository.GetByCodeAsync(bearingDto.BrandCode, cancellationToken);
                    if (brand == null)
                    {
                        result.AddFailed(bearingDto.PartNumber, $"品牌不存在: {bearingDto.BrandCode}");
                        continue;
                    }

                    // 验证轴承类型是否存在
                    var bearingType = await _bearingTypeRepository.GetByCodeAsync(bearingDto.TypeName, cancellationToken);
                    if (bearingType == null)
                    {
                        result.AddFailed(bearingDto.PartNumber, $"轴承类型不存在: {bearingDto.TypeName}");
                        continue;
                    }

                    // 检查轴承是否已存在
                    var existingBearing = await _bearingRepository.GetByPartNumberAsync(bearingDto.PartNumber, cancellationToken);

                    if (existingBearing != null && request.Mode == SyncMode.Create)
                    {
                        // 已存在但不是爬虫数据，不覆盖（保护纠错/人工导入/Excel导入的数据）
                        if (existingBearing.DataSource?.SourceType != OpenFindBearings.Domain.Enums.DataSourceType.Crawler)
                        {
                            result.AddSkipped(bearingDto.PartNumber, "非爬虫数据，跳过覆盖保护");
                            _logger.LogDebug("跳过覆盖: {Code}, 数据来源: {Source}",
                                bearingDto.PartNumber, existingBearing.DataSource?.SourceType);
                            continue;
                        }

                        result.AddFailed(bearingDto.PartNumber, "轴承型号已存在");
                        continue;
                    }

                    if (existingBearing == null && request.Mode == SyncMode.Update)
                    {
                        result.AddFailed(bearingDto.PartNumber, "轴承型号不存在");
                        continue;
                    }

                    // 更新时也要检查是否为非爬虫数据
                    if (existingBearing != null && existingBearing.DataSource?.SourceType != OpenFindBearings.Domain.Enums.DataSourceType.Crawler)
                    {
                        result.AddSkipped(bearingDto.PartNumber, "非爬虫数据，跳过覆盖保护");
                        _logger.LogDebug("跳过更新: {Code}, 数据来源: {Source}",
                            bearingDto.PartNumber, existingBearing.DataSource?.SourceType);
                        continue;
                    }

                    // 执行创建或更新
                    if (existingBearing == null)
                    {
                        // 创建新轴承
                        var createCommand = new CreateBearingCommand
                        {
                            PartNumber = bearingDto.PartNumber,
                            OldNumber = bearingDto.OldNumber,
                            CodeSource = bearingDto.CodeSource,
                            Description = bearingDto.Description,
                            BearingType = bearingType.Name,
                            StructureType = bearingDto.StructureType,
                            SizeSeries = bearingDto.SizeSeries,
                            IsStandard = bearingDto.IsStandard,
                            InnerDiameter = bearingDto.InnerDiameter,
                            OuterDiameter = bearingDto.OuterDiameter,
                            Width = bearingDto.Width,
                            ChamferRmin = bearingDto.ChamferRmin,
                            ChamferRmax = bearingDto.ChamferRmax,
                            Weight = bearingDto.Weight,
                            PrecisionGrade = bearingDto.PrecisionGrade,
                            Material = bearingDto.Material,
                            SealType = bearingDto.SealType,
                            CageType = bearingDto.CageType,
                            DynamicLoad = bearingDto.DynamicLoad,
                            StaticLoad = bearingDto.StaticLoad,
                            LimitingSpeed = bearingDto.LimitingSpeed,
                            LimitingSpeedGrease = bearingDto.LimitingSpeedGrease,
                            LimitingSpeedOil = bearingDto.LimitingSpeedOil,
                            Image3DUrl = bearingDto.Image3DUrl,
                            Image2DUrl = bearingDto.Image2DUrl,
                            BearingTypeId = bearingType.Id,
                            BrandId = brand.Id,
                            Trademark = bearingDto.Trademark,
                            OriginCountry = bearingDto.OriginCountry,
                            Category = bearingDto.Category,
                            SourceSite = bearingDto.SourceSite,
                            DataSource = bearingDto.DataSource
                        };

                        var id = await _mediator.Send(createCommand, cancellationToken);
                        result.AddSuccess(bearingDto.PartNumber, "created", id);
                    }
                    else if (request.Mode == SyncMode.Update || request.Mode == SyncMode.Upsert)
                    {
                        // 更新现有轴承
                        var updateCommand = new UpdateBearingCommand
                        {
                            Id = existingBearing.Id,
                            OldNumber = bearingDto.OldNumber,
                            CodeSource = bearingDto.CodeSource,
                            Description = bearingDto.Description,
                            StructureType = bearingDto.StructureType,
                            SizeSeries = bearingDto.SizeSeries,
                            ChamferRmin = bearingDto.ChamferRmin,
                            ChamferRmax = bearingDto.ChamferRmax,
                            Weight = bearingDto.Weight,
                            PrecisionGrade = bearingDto.PrecisionGrade,
                            Material = bearingDto.Material,
                            SealType = bearingDto.SealType,
                            CageType = bearingDto.CageType,
                            DynamicLoad = bearingDto.DynamicLoad,
                            StaticLoad = bearingDto.StaticLoad,
                            LimitingSpeed = bearingDto.LimitingSpeed,
                            LimitingSpeedGrease = bearingDto.LimitingSpeedGrease,
                            LimitingSpeedOil = bearingDto.LimitingSpeedOil,
                            Image3DUrl = bearingDto.Image3DUrl,
                            Image2DUrl = bearingDto.Image2DUrl,
                            Trademark = bearingDto.Trademark,
                            OriginCountry = bearingDto.OriginCountry,
                            Category = bearingDto.Category
                        };
                        await _mediator.Send(updateCommand, cancellationToken);
                        result.AddSuccess(bearingDto.PartNumber, "updated", existingBearing.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "批量创建轴承失败: {CurrentCode}", bearingDto.PartNumber);
                    result.AddFailed(bearingDto.PartNumber, ex.Message);
                }
            }

            _logger.LogInformation("批量创建轴承完成，成功: {SuccessCount}, 失败: {FailCount}",
                result.SuccessCount, result.FailCount);

            return result;
        }
    }
}
