using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.BearingTypes.DTOs;
using OpenFindBearings.Application.Features.BearingTypes.Queries;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.BearingTypes.Handlers
{
    /// <summary>
    /// 获取单个轴承类型查询处理器
    /// </summary>
    public class GetBearingTypeQueryHandler : IRequestHandler<GetBearingTypeQuery, BearingTypeDto?>
    {
        private readonly IBearingTypeRepository _bearingTypeRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly ILogger<GetBearingTypeQueryHandler> _logger;

        public GetBearingTypeQueryHandler(
            IBearingTypeRepository bearingTypeRepository,
            IBearingRepository bearingRepository,
            ILogger<GetBearingTypeQueryHandler> logger)
        {
            _bearingTypeRepository = bearingTypeRepository;
            _bearingRepository = bearingRepository;
            _logger = logger;
        }

        public async Task<BearingTypeDto?> Handle(GetBearingTypeQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取轴承类型详情: Id={Id}", request.Id);

            var bearingType = await _bearingTypeRepository.GetByIdAsync(request.Id, cancellationToken);
            if (bearingType == null) return null;

            var bearingCountByType = await _bearingRepository.GetBearingCountByTypeAsync(cancellationToken);
            var bearingCount = bearingCountByType.GetValueOrDefault(bearingType.Id, 0);

            return new BearingTypeDto
            {
                Id = bearingType.Id,
                Code = bearingType.Code,
                Name = bearingType.Name,
                Description = bearingType.Description,
                BearingCount = bearingCount
            };
        }
    }
}
