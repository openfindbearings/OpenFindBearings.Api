using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Application.Commands.Bearings.CreateBearing
{
    public class CreateBearingCommandHandler : IRequestHandler<CreateBearingCommand, Guid>
    {
        private readonly IBearingRepository _bearingRepository;
        private readonly IValidator<CreateBearingCommand> _validator;
        private readonly ILogger<CreateBearingCommandHandler> _logger;

        public CreateBearingCommandHandler(
            IBearingRepository bearingRepository,
            IValidator<CreateBearingCommand> validator,
            ILogger<CreateBearingCommandHandler> logger)
        {
            _bearingRepository = bearingRepository;
            _validator = validator;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateBearingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始处理创建轴承命令: {PartNumber}", request.PartNumber);

            await ValidateCommandAsync(request, cancellationToken);
            await CheckDuplicatePartNumberAsync(request.PartNumber, cancellationToken);

            var dimensions = CreateDimensions(request);
            var performance = CreatePerformanceParams(request);

            var bearing = CreateBearingEntity(request, dimensions, performance);
            SetTechnicalParameters(bearing, request);
            SetIdentification(bearing, request);
            bearing.SetOrigin(request.OriginCountry, request.Category);
            bearing.SetImages(request.Image3DUrl, request.Image2DUrl);
            SetDataSource(bearing, request);

            await _bearingRepository.AddAsync(bearing, cancellationToken);

            _logger.LogInformation("轴承创建成功: {BearingId}, 型号: {PartNumber}", bearing.Id, bearing.PartNumber);

            return bearing.Id;
        }

        private async Task ValidateCommandAsync(CreateBearingCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
        }

        private async Task CheckDuplicatePartNumberAsync(string partNumber, CancellationToken cancellationToken)
        {
            var exists = await _bearingRepository.ExistsByPartNumberAsync(partNumber, cancellationToken);
            if (exists)
            {
                throw new InvalidOperationException($"轴承型号 {partNumber} 已存在");
            }
        }

        private Dimensions CreateDimensions(CreateBearingCommand request)
        {
            return new Dimensions(request.InnerDiameter, request.OuterDiameter, request.Width);
        }

        private PerformanceParams? CreatePerformanceParams(CreateBearingCommand request)
        {
            if (!request.DynamicLoad.HasValue && !request.StaticLoad.HasValue && !request.LimitingSpeed.HasValue && !request.LimitingSpeedGrease.HasValue && !request.LimitingSpeedOil.HasValue)
                return null;

            return new PerformanceParams(request.DynamicLoad, request.StaticLoad, request.LimitingSpeed, request.LimitingSpeedGrease, request.LimitingSpeedOil);
        }

        private Bearing CreateBearingEntity(CreateBearingCommand request, Dimensions dimensions, PerformanceParams? performance)
        {
            if (request.IsStandard)
            {
                return new Bearing(
                    partNumber: request.PartNumber,
                    bearingTypeId: request.BearingTypeId,
                    bearingType: request.BearingType,
                    dimensions: dimensions,
                    brandId: request.BrandId,
                    performance: performance,
                    weight: request.Weight);
            }
            else
            {
                return Bearing.CreateNonStandard(
                    partNumber: request.PartNumber,
                    bearingTypeId: request.BearingTypeId,
                    bearingType: request.BearingType,
                    dimensions: dimensions,
                    brandId: request.BrandId,
                    structureType: request.StructureType,
                    sizeSeries: request.SizeSeries,
                    performance: performance,
                    weight: request.Weight);
            }
        }

        private void SetTechnicalParameters(Bearing bearing, CreateBearingCommand request)
        {
            bearing.UpdateTechnicalSpecs(
                precisionGrade: request.PrecisionGrade,
                material: request.Material,
                sealType: request.SealType,
                cageType: request.CageType);

            bearing.UpdateDimensionDetails(request.ChamferRmin, request.ChamferRmax);
            bearing.UpdateDetails(request.Description, request.Weight);
        }

        private void SetIdentification(Bearing bearing, CreateBearingCommand request)
        {
            bearing.UpdateIdentification(request.OldNumber, request.CodeSource, request.Trademark);
        }

        private void SetDataSource(Bearing bearing, CreateBearingCommand request)
        {
            var sourceType = request.DataSource ?? "manual";
            var importedBy = request.SourceSite ?? request.ImportedBy;

            if (sourceType.Equals("crawler", StringComparison.OrdinalIgnoreCase))
            {
                bearing.SetDataSource(DataSource.FromCrawler(importedBy ?? "unknown"));
            }
            else if (sourceType.Equals("api", StringComparison.OrdinalIgnoreCase))
            {
                bearing.SetDataSource(DataSource.FromApi(importedBy ?? "ApiSync"));
            }
            else if (sourceType.Equals("file", StringComparison.OrdinalIgnoreCase) || sourceType.Equals("fileimport", StringComparison.OrdinalIgnoreCase))
            {
                bearing.SetDataSource(DataSource.FromFileImport(importedBy));
            }
            else if (sourceType.Equals("seeddata", StringComparison.OrdinalIgnoreCase) || sourceType.Equals("seed", StringComparison.OrdinalIgnoreCase))
            {
                bearing.SetDataSource(DataSource.FromSeedData());
            }
            else
            {
                bearing.SetDataSource(DataSource.FromManual(importedBy));
            }
        }
    }
}

