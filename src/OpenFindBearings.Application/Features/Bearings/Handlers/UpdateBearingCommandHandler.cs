using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Bearings.Commands;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Bearings.Handlers
{
    /// <summary>
    /// 更新轴承命令处理器
    /// </summary>
    public class UpdateBearingCommandHandler : IRequestHandler<UpdateBearingCommand>
    {
        private readonly IBearingRepository _bearingRepository;
        private readonly IMediator _mediator;
        private readonly ILogger<UpdateBearingCommandHandler> _logger;

        public UpdateBearingCommandHandler(
            IBearingRepository bearingRepository,
            IMediator mediator,
            ILogger<UpdateBearingCommandHandler> logger)
        {
            _bearingRepository = bearingRepository;
            _mediator = mediator;
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

            // 记录修改的字段
            var changedFields = new List<string>();

            if (request.Name != null)
            {
                // bearing.UpdateName(request.Name);
                changedFields.Add("Name");
            }

            if (request.Description != null)
            {
                bearing.UpdateDetails(request.Description, request.Weight);
                changedFields.Add("Description");
            }

            // 更新产地和类别
            if (request.OriginCountry != null)
            {
                // bearing.SetOriginCountry(request.OriginCountry);
                changedFields.Add("OriginCountry");
            }

            if (request.Category.HasValue)
            {
                // bearing.SetCategory(request.Category.Value);
                changedFields.Add("Category");
            }

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

            await _bearingRepository.UpdateAsync(bearing, cancellationToken);

            // 发布更新事件
            await _mediator.Publish(new BearingUpdatedEvent(
                bearing.Id,
                bearing.PartNumber,
                changedFields
            ), cancellationToken);

            _logger.LogInformation("轴承更新成功: {BearingId}", bearing.Id);
        }
    }
}
