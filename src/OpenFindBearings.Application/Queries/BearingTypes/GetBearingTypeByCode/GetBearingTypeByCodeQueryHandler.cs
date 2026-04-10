using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.BearingTypes.GetBearingTypeByCode
{
    /// <summary>
    /// 根据代码获取轴承类型查询处理器
    /// </summary>
    public class GetBearingTypeByCodeQueryHandler : IRequestHandler<GetBearingTypeByCodeQuery, BearingTypeDto?>
    {
        private readonly IBearingTypeRepository _bearingTypeRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly ILogger<GetBearingTypeByCodeQueryHandler> _logger;

        public GetBearingTypeByCodeQueryHandler(
            IBearingTypeRepository bearingTypeRepository,
            IBearingRepository bearingRepository,
            ILogger<GetBearingTypeByCodeQueryHandler> logger)
        {
            _bearingTypeRepository = bearingTypeRepository;
            _bearingRepository = bearingRepository;
            _logger = logger;
        }

        public async Task<BearingTypeDto?> Handle(GetBearingTypeByCodeQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("根据代码获取轴承类型: Code={Code}", request.Code);

            var bearingType = await _bearingTypeRepository.GetByCodeAsync(request.Code, cancellationToken);
            if (bearingType == null) return null;

            var bearingCountByType = await _bearingRepository.GetBearingCountByTypeAsync(cancellationToken);
            var bearingCount = bearingCountByType.GetValueOrDefault(bearingType.Id, 0);

            return bearingType.ToDto(bearingCount);
        }
    }
}
