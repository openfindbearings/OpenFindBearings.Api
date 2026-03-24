using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.BearingTypes.DTOs;
using OpenFindBearings.Application.Features.BearingTypes.Queries;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.BearingTypes.Handlers
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

            return bearingTypes.Select(bt => new BearingTypeDto
            {
                Id = bt.Id,
                Code = bt.Code,
                Name = bt.Name,
                Description = bt.Description,
                BearingCount = bearingCountByType.GetValueOrDefault(bt.Id, 0)
            }).ToList();
        }
    }
}
