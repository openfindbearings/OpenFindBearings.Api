using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Bearings.Commands;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Application.Features.Bearings.Handlers
{
    /// <summary>
    /// 创建轴承命令处理器
    /// </summary>
    public class CreateBearingCommandHandler : IRequestHandler<CreateBearingCommand, Guid>
    {
        private readonly IBearingRepository _bearingRepository;
        private readonly IValidator<CreateBearingCommand> _validator;
        private readonly IMediator _mediator;
        private readonly ILogger<CreateBearingCommandHandler> _logger;

        public CreateBearingCommandHandler(
            IBearingRepository bearingRepository,
            IValidator<CreateBearingCommand> validator,
            IMediator mediator,
            ILogger<CreateBearingCommandHandler> logger)
        {
            _bearingRepository = bearingRepository;
            _validator = validator;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateBearingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始处理创建轴承命令: {CurrentCode}", request.CurrentCode);

            await ValidateCommandAsync(request, cancellationToken);
            await CheckDuplicateCurrentCodeAsync(request.CurrentCode, cancellationToken);

            var dimensions = CreateDimensions(request);
            var performance = CreatePerformanceParams(request);

            var bearing = CreateBearingEntity(request, dimensions, performance);
            SetTechnicalParameters(bearing, request);
            SetIdentification(bearing, request);
            bearing.SetOrigin(request.OriginCountry, request.Category);

            await _bearingRepository.AddAsync(bearing, cancellationToken);

            await PublishBearingCreatedEventAsync(bearing, cancellationToken);

            _logger.LogInformation("轴承创建成功: {BearingId}, 型号: {CurrentCode}", bearing.Id, bearing.CurrentCode);

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

        private async Task CheckDuplicateCurrentCodeAsync(string currentCode, CancellationToken cancellationToken)
        {
            var exists = await _bearingRepository.ExistsByPartNumberAsync(currentCode, cancellationToken);
            if (exists)
            {
                throw new InvalidOperationException($"轴承型号 {currentCode} 已存在");
            }
        }

        private Dimensions CreateDimensions(CreateBearingCommand request)
        {
            return new Dimensions(request.InnerDiameter, request.OuterDiameter, request.Width);
        }

        private PerformanceParams? CreatePerformanceParams(CreateBearingCommand request)
        {
            if (!request.DynamicLoadRating.HasValue && !request.StaticLoadRating.HasValue && !request.LimitingSpeed.HasValue)
                return null;

            return new PerformanceParams(request.DynamicLoadRating, request.StaticLoadRating, request.LimitingSpeed);
        }

        private Bearing CreateBearingEntity(CreateBearingCommand request, Dimensions dimensions, PerformanceParams? performance)
        {
            if (request.IsStandard)
            {
                return new Bearing(
                    currentCode: request.CurrentCode,
                    name: request.Name,
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
                    currentCode: request.CurrentCode,
                    name: request.Name,
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
            bearing.UpdateIdentification(request.FormerCode, request.CodeSource, request.Trademark);
        }

        private async Task PublishBearingCreatedEventAsync(Bearing bearing, CancellationToken cancellationToken)
        {
            var domainEvent = new BearingCreatedEvent(bearing.Id, bearing.CurrentCode, bearing.BrandId);
            await _mediator.Publish(domainEvent, cancellationToken);
        }
    }
}
