using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.BearingTypes.GetAllBearingTypes
{
    /// <summary>
    /// 获取所有轴承类型列表查询处理器
    /// </summary>
    public class GetAllBearingTypesQueryHandler : IRequestHandler<GetAllBearingTypesQuery, List<BearingTypeDto>>
    {
        private readonly IBearingTypeRepository _bearingTypeRepository;
        private readonly IBearingRepository _bearingRepository;
        private readonly ILogger<GetAllBearingTypesQueryHandler> _logger;

        public GetAllBearingTypesQueryHandler(
            IBearingTypeRepository bearingTypeRepository,
            IBearingRepository bearingRepository,
            ILogger<GetAllBearingTypesQueryHandler> logger)
        {
            _bearingTypeRepository = bearingTypeRepository;
            _bearingRepository = bearingRepository;
            _logger = logger;
        }

        public async Task<List<BearingTypeDto>> Handle(GetAllBearingTypesQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取所有轴承类型列表");

            var bearingTypes = await _bearingTypeRepository.GetAllAsync(cancellationToken);

            var bearingCountByType = await _bearingRepository.GetBearingCountByTypeAsync(cancellationToken);

            return bearingTypes.Select(bt => bt.ToDto(bearingCountByType.GetValueOrDefault(bt.Id))).ToList();
        }
    }
}
