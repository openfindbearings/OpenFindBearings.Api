using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Bearings.Commands;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.Interfaces;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Application.Features.Bearings.Handlers
{
    /// <summary>
    /// 创建轴承命令处理器
    /// 处理创建新轴承的业务逻辑
    /// </summary>
    public class CreateBearingCommandHandler : IRequestHandler<CreateBearingCommand, Guid>
    {
        private readonly IBearingRepository _bearingRepository;
        private readonly IValidator<CreateBearingCommand> _validator;
        private readonly IMediator _mediator;
        private readonly ILogger<CreateBearingCommandHandler> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="bearingRepository">轴承仓储</param>
        /// <param name="validator">命令验证器</param>
        /// <param name="mediator">中介者（用于发布领域事件）</param>
        /// <param name="logger">日志记录器</param>
        public CreateBearingCommandHandler(
            IBearingRepository bearingRepository,
            IValidator<CreateBearingCommand> validator,
            IMediator mediator,
            ILogger<CreateBearingCommandHandler> logger)
        {
            _bearingRepository = bearingRepository ?? throw new ArgumentNullException(nameof(bearingRepository));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 处理创建轴承命令
        /// </summary>
        /// <param name="request">创建轴承命令</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>新创建的轴承ID</returns>
        /// <exception cref="ValidationException">验证失败时抛出</exception>
        /// <exception cref="InvalidOperationException">型号已存在时抛出</exception>
        public async Task<Guid> Handle(
            CreateBearingCommand request,
            CancellationToken cancellationToken)
        {
            // 1. 记录处理开始
            _logger.LogInformation("开始处理创建轴承命令: {PartNumber}", request.PartNumber);

            // 2. 验证命令
            await ValidateCommandAsync(request, cancellationToken);

            // 3. 检查型号是否已存在
            await CheckDuplicatePartNumberAsync(request.PartNumber, cancellationToken);

            // 4. 创建值对象
            var dimensions = CreateDimensions(request);
            var performance = CreatePerformanceParams(request);

            // 5. 创建轴承实体
            var bearing = CreateBearingEntity(request, dimensions, performance);

            // 6. 设置技术参数
            SetTechnicalParameters(bearing, request);

            // 7. 设置产地和类别
            bearing.SetOrigin(request.OriginCountry, request.Category);

            // 8. 保存到仓储
            await SaveBearingAsync(bearing, cancellationToken);

            // 9. 发布领域事件
            await PublishBearingCreatedEventAsync(bearing, cancellationToken);

            // 10. 返回结果
            _logger.LogInformation("轴承创建成功: {BearingId}, 型号: {PartNumber}",
                bearing.Id, bearing.PartNumber);

            return bearing.Id;
        }

        /// <summary>
        /// 验证命令
        /// </summary>
        private async Task ValidateCommandAsync(CreateBearingCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("轴承创建命令验证失败: {Errors}",
                    string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                throw new ValidationException(validationResult.Errors);
            }
        }

        /// <summary>
        /// 检查型号是否重复
        /// </summary>
        private async Task CheckDuplicatePartNumberAsync(string partNumber, CancellationToken cancellationToken)
        {
            var exists = await _bearingRepository.ExistsAsync(partNumber, cancellationToken);
            if (exists)
            {
                _logger.LogWarning("轴承型号已存在: {PartNumber}", partNumber);
                throw new InvalidOperationException($"轴承型号 {partNumber} 已存在");
            }
        }

        /// <summary>
        /// 创建尺寸值对象
        /// </summary>
        private Dimensions CreateDimensions(CreateBearingCommand request)
        {
            try
            {
                return new Dimensions(
                    request.InnerDiameter,
                    request.OuterDiameter,
                    request.Width);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "尺寸参数验证失败");
                throw;
            }
        }

        /// <summary>
        /// 创建性能参数值对象
        /// </summary>
        private PerformanceParams? CreatePerformanceParams(CreateBearingCommand request)
        {
            if (!request.DynamicLoadRating.HasValue &&
                !request.StaticLoadRating.HasValue &&
                !request.LimitingSpeed.HasValue)
            {
                return null;
            }

            try
            {
                return new PerformanceParams(
                    request.DynamicLoadRating,
                    request.StaticLoadRating,
                    request.LimitingSpeed);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "性能参数验证失败");
                throw;
            }
        }

        /// <summary>
        /// 创建轴承实体
        /// </summary>
        private Bearing CreateBearingEntity(
            CreateBearingCommand request,
            Dimensions dimensions,
            PerformanceParams? performance)
        {
            return new Bearing(
                partNumber: request.PartNumber,
                name: request.Name,
                dimensions: dimensions,
                bearingTypeId: request.BearingTypeId,
                brandId: request.BrandId,
                performance: performance,
                weight: request.Weight);
        }

        /// <summary>
        /// 设置技术参数
        /// </summary>
        private void SetTechnicalParameters(Bearing bearing, CreateBearingCommand request)
        {
            // 只有当至少有一个技术参数时才调用更新方法
            if (request.PrecisionGrade != null ||
                request.Material != null ||
                request.SealType != null ||
                request.CageType != null)
            {
                bearing.UpdateTechnicalSpecs(
                    precisionGrade: request.PrecisionGrade,
                    material: request.Material,
                    sealType: request.SealType,
                    cageType: request.CageType);
            }

            // 设置描述（如果有）
            if (!string.IsNullOrWhiteSpace(request.Description))
            {
                bearing.UpdateDetails(
                    description: request.Description,
                    weight: request.Weight);
            }
        }

        /// <summary>
        /// 保存轴承
        /// </summary>
        private async Task SaveBearingAsync(Bearing bearing, CancellationToken cancellationToken)
        {
            try
            {
                await _bearingRepository.AddAsync(bearing, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存轴承失败: {PartNumber}", bearing.PartNumber);
                throw new InvalidOperationException($"保存轴承失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 发布轴承创建事件
        /// </summary>
        private async Task PublishBearingCreatedEventAsync(Bearing bearing, CancellationToken cancellationToken)
        {
            try
            {
                var domainEvent = new BearingCreatedEvent(
                    bearingId: bearing.Id,
                    partNumber: bearing.PartNumber,
                    brandId: bearing.BrandId);

                await _mediator.Publish(domainEvent, cancellationToken);

                _logger.LogDebug("已发布轴承创建事件: {BearingId}", bearing.Id);
            }
            catch (Exception ex)
            {
                // 事件发布失败不影响主流程，只记录日志
                _logger.LogWarning(ex, "发布轴承创建事件失败: {BearingId}", bearing.Id);
            }
        }
    }
}
