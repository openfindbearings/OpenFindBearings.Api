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
                        result.AddFailed(bearingDto.CurrentCode, $"品牌不存在: {bearingDto.BrandCode}");
                        continue;
                    }

                    // 验证轴承类型是否存在
                    var bearingType = await _bearingTypeRepository.GetByCodeAsync(bearingDto.BearingTypeCode, cancellationToken);
                    if (bearingType == null)
                    {
                        result.AddFailed(bearingDto.CurrentCode, $"轴承类型不存在: {bearingDto.BearingTypeCode}");
                        continue;
                    }

                    // 检查轴承是否已存在
                    var existingBearing = await _bearingRepository.GetByPartNumberAsync(bearingDto.CurrentCode, cancellationToken);

                    if (existingBearing != null && request.Mode == SyncMode.Create)
                    {
                        // 已存在但不是爬虫数据，不覆盖（保护纠错/人工导入/Excel导入的数据）
                        if (existingBearing.DataSource?.SourceType != OpenFindBearings.Domain.Enums.DataSourceType.Crawler)
                        {
                            result.AddSkipped(bearingDto.CurrentCode, "非爬虫数据，跳过覆盖保护");
                            _logger.LogDebug("跳过覆盖: {Code}, 数据来源: {Source}",
                                bearingDto.CurrentCode, existingBearing.DataSource?.SourceType);
                            continue;
                        }

                        result.AddFailed(bearingDto.CurrentCode, "轴承型号已存在");
                        continue;
                    }

                    if (existingBearing == null && request.Mode == SyncMode.Update)
                    {
                        result.AddFailed(bearingDto.CurrentCode, "轴承型号不存在");
                        continue;
                    }

                    // 更新时也要检查是否为非爬虫数据
                    if (existingBearing != null && existingBearing.DataSource?.SourceType != OpenFindBearings.Domain.Enums.DataSourceType.Crawler)
                    {
                        result.AddSkipped(bearingDto.CurrentCode, "非爬虫数据，跳过覆盖保护");
                        _logger.LogDebug("跳过更新: {Code}, 数据来源: {Source}",
                            bearingDto.CurrentCode, existingBearing.DataSource?.SourceType);
                        continue;
                    }

                    // 执行创建或更新
                    if (existingBearing == null)
                    {
                        // 创建新轴承
                        var createCommand = new CreateBearingCommand
                        {
                            CurrentCode = bearingDto.CurrentCode,
                            FormerCode = bearingDto.FormerCode,
                            CodeSource = bearingDto.CodeSource,
                            Name = bearingDto.Name,
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
                            DynamicLoadRating = bearingDto.DynamicLoadRating,
                            StaticLoadRating = bearingDto.StaticLoadRating,
                            LimitingSpeed = bearingDto.LimitingSpeed,
                            LimitingSpeedGrease = bearingDto.LimitingSpeedGrease,
                            LimitingSpeedOil = bearingDto.LimitingSpeedOil,
                            Image3D = bearingDto.Image3D,
                            Image2DCAD = bearingDto.Image2DCAD,
                            BearingTypeId = bearingType.Id,
                            BrandId = brand.Id,
                            Trademark = bearingDto.Trademark,
                            OriginCountry = bearingDto.OriginCountry,
                            Category = bearingDto.Category
                        };

                        var id = await _mediator.Send(createCommand, cancellationToken);
                        result.AddSuccess(bearingDto.CurrentCode, "created", id);
                    }
                    else if (request.Mode == SyncMode.Update || request.Mode == SyncMode.Upsert)
                    {
                        // 更新现有轴承
                        var updateCommand = new UpdateBearingCommand
                        {
                            Id = existingBearing.Id,
                            Name = bearingDto.Name,
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
                            DynamicLoadRating = bearingDto.DynamicLoadRating,
                            StaticLoadRating = bearingDto.StaticLoadRating,
                            LimitingSpeed = bearingDto.LimitingSpeed,
                            LimitingSpeedGrease = bearingDto.LimitingSpeedGrease,
                            LimitingSpeedOil = bearingDto.LimitingSpeedOil,
                            Image3D = bearingDto.Image3D,
                            Image2DCAD = bearingDto.Image2DCAD,
                            Trademark = bearingDto.Trademark,
                            OriginCountry = bearingDto.OriginCountry,
                            Category = bearingDto.Category
                        };
                        await _mediator.Send(updateCommand, cancellationToken);
                        result.AddSuccess(bearingDto.CurrentCode, "updated", existingBearing.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "批量创建轴承失败: {CurrentCode}", bearingDto.CurrentCode);
                    result.AddFailed(bearingDto.CurrentCode, ex.Message);
                }
            }

            _logger.LogInformation("批量创建轴承完成，成功: {SuccessCount}, 失败: {FailCount}",
                result.SuccessCount, result.FailCount);

            return result;
        }
    }
}
