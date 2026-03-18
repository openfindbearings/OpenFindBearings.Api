using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Common.Models;
using OpenFindBearings.Application.Features.Bearings.Commands;
using OpenFindBearings.Application.Features.Sync.Commands;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Sync.Handlers
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
                    // 验证品牌和类型是否存在
                    var brand = await _brandRepository.GetByCodeAsync(bearingDto.BrandCode, cancellationToken);
                    if (brand == null)
                    {
                        result.AddFailed(bearingDto.PartNumber, $"品牌不存在: {bearingDto.BrandCode}");
                        continue;
                    }

                    var bearingType = await _bearingTypeRepository.GetByCodeAsync(bearingDto.BearingTypeCode, cancellationToken);
                    if (bearingType == null)
                    {
                        result.AddFailed(bearingDto.PartNumber, $"轴承类型不存在: {bearingDto.BearingTypeCode}");
                        continue;
                    }

                    // 检查轴承是否已存在
                    var existingBearing = await _bearingRepository.GetByPartNumberAsync(bearingDto.PartNumber, cancellationToken);

                    if (existingBearing != null && request.Mode == Common.Enums.SyncMode.Create)
                    {
                        result.AddFailed(bearingDto.PartNumber, "轴承型号已存在");
                        continue;
                    }

                    if (existingBearing == null && request.Mode == Common.Enums.SyncMode.Update)
                    {
                        result.AddFailed(bearingDto.PartNumber, "轴承型号不存在");
                        continue;
                    }

                    // 执行创建或更新
                    if (existingBearing == null)
                    {
                        // 创建新轴承
                        var createCommand = new CreateBearingCommand
                        {
                            PartNumber = bearingDto.PartNumber,
                            Name = bearingDto.Name,
                            Description = bearingDto.Description,
                            InnerDiameter = bearingDto.InnerDiameter,
                            OuterDiameter = bearingDto.OuterDiameter,
                            Width = bearingDto.Width,
                            Weight = bearingDto.Weight,
                            PrecisionGrade = bearingDto.PrecisionGrade,
                            Material = bearingDto.Material,
                            SealType = bearingDto.SealType,
                            CageType = bearingDto.CageType,
                            DynamicLoadRating = bearingDto.DynamicLoadRating,
                            StaticLoadRating = bearingDto.StaticLoadRating,
                            LimitingSpeed = bearingDto.LimitingSpeed,
                            BearingTypeId = bearingType.Id,
                            BrandId = brand.Id
                        };

                        var id = await _mediator.Send(createCommand, cancellationToken);
                        result.AddSuccess(bearingDto.PartNumber, "created", id);
                    }
                    else if (request.Mode == Common.Enums.SyncMode.Update || request.Mode == Common.Enums.SyncMode.Upsert)
                    {
                        // 更新现有轴承
                        // 这里需要实现 UpdateBearingCommand
                        var updateCommand = new UpdateBearingCommand
                        {
                            Id = existingBearing.Id,
                            Name = bearingDto.Name,
                            Description = bearingDto.Description,
                            Weight = bearingDto.Weight,
                            PrecisionGrade = bearingDto.PrecisionGrade,
                            Material = bearingDto.Material,
                            SealType = bearingDto.SealType,
                            CageType = bearingDto.CageType,
                            DynamicLoadRating = bearingDto.DynamicLoadRating,
                            StaticLoadRating = bearingDto.StaticLoadRating,
                            LimitingSpeed = bearingDto.LimitingSpeed
                        };
                        await _mediator.Send(updateCommand, cancellationToken);
                        result.AddSuccess(bearingDto.PartNumber, "updated", existingBearing.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "批量创建轴承失败: {PartNumber}", bearingDto.PartNumber);
                    result.AddFailed(bearingDto.PartNumber, ex.Message);
                }
            }

            _logger.LogInformation("批量创建轴承完成，成功: {SuccessCount}, 失败: {FailCount}",
                result.SuccessCount, result.FailCount);

            return result;
        }
    }
}
