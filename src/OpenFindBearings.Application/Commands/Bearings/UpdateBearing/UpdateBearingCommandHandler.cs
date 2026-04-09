using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Application.Commands.Bearings.UpdateBearing
{
    public class UpdateBearingCommandHandler : IRequestHandler<UpdateBearingCommand>
    {
        private readonly IBearingRepository _bearingRepository;
        private readonly ILogger<UpdateBearingCommandHandler> _logger;

        public UpdateBearingCommandHandler(
            IBearingRepository bearingRepository,
            ILogger<UpdateBearingCommandHandler> logger)
        {
            _bearingRepository = bearingRepository;
            _logger = logger;
        }

        public async Task Handle(UpdateBearingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始更新轴承: {BearingId}", request.Id);

            var bearing = await _bearingRepository.GetByIdAsync(request.Id, cancellationToken);
            if (bearing == null)
            {
                throw new InvalidOperationException($"轴承不存在: {request.Id}");
            }

            var changedFields = new List<string>();

            // 更新基本信息
            if (request.Name != null)
            {
                // bearing.UpdateName(request.Name);
                changedFields.Add("Name");
            }

            // 更新描述和重量
            if (request.Description != null || request.Weight.HasValue)
            {
                bearing.UpdateDetails(request.Description, request.Weight);
                changedFields.Add("Description");
                if (request.Weight.HasValue) changedFields.Add("Weight");
            }

            // 更新尺寸相关参数
            if (request.ChamferRmin.HasValue || request.ChamferRmax.HasValue)
            {
                bearing.UpdateDimensionDetails(request.ChamferRmin, request.ChamferRmax);
                changedFields.Add("ChamferRmin");
                changedFields.Add("ChamferRmax");
            }

            // 更新结构类型
            if (request.StructureType != null)
            {
                // bearing.UpdateStructureType(request.StructureType);
                changedFields.Add("StructureType");
            }

            // 更新尺寸系列
            if (request.SizeSeries != null)
            {
                // bearing.UpdateSizeSeries(request.SizeSeries);
                changedFields.Add("SizeSeries");
            }

            // 更新技术参数
            if (request.PrecisionGrade != null || request.Material != null ||
                request.SealType != null || request.CageType != null)
            {
                bearing.UpdateTechnicalSpecs(
                    request.PrecisionGrade,
                    request.Material,
                    request.SealType,
                    request.CageType);
                changedFields.AddRange(["PrecisionGrade", "Material", "SealType", "CageType"]);
            }

            // 更新性能参数
            if (request.DynamicLoadRating.HasValue || request.StaticLoadRating.HasValue || request.LimitingSpeed.HasValue)
            {
                var performance = new PerformanceParams(
                    request.DynamicLoadRating,
                    request.StaticLoadRating,
                    request.LimitingSpeed);
                bearing.UpdatePerformance(performance);
                changedFields.Add("Performance");
            }

            // 更新商标
            if (request.Trademark != null)
            {
                bearing.UpdateIdentification(null, null, request.Trademark);
                changedFields.Add("Trademark");
            }

            // 更新产地和类别
            if (request.OriginCountry != null)
            {
                bearing.SetOrigin(request.OriginCountry, request.Category ?? bearing.Category);
                changedFields.Add("OriginCountry");
            }
            else if (request.Category.HasValue)
            {
                bearing.SetOrigin(bearing.OriginCountry, request.Category.Value);
                changedFields.Add("Category");
            }

            await _bearingRepository.UpdateAsync(bearing, cancellationToken);

            _logger.LogInformation("轴承更新成功: {BearingId}", bearing.Id);
        }
    }
}
