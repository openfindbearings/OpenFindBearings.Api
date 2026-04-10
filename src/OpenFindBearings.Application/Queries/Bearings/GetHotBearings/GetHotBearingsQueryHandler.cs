using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Extensions;
using OpenFindBearings.Application.Interfaces;
using OpenFindBearings.Application.Shared.Constants;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Bearings.GetHotBearings
{
    public class GetHotBearingsQueryHandler : IRequestHandler<GetHotBearingsQuery, List<BearingDto>>
    {
        private readonly IBearingRepository _bearingRepository;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetHotBearingsQueryHandler> _logger;

        public GetHotBearingsQueryHandler(
            IBearingRepository bearingRepository,
            ICacheService cacheService,
            ILogger<GetHotBearingsQueryHandler> logger)
        {
            _bearingRepository = bearingRepository;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<List<BearingDto>> Handle(GetHotBearingsQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.GetHotBearingsKey(request.Count);

            var cached = await _cacheService.GetAsync<List<BearingDto>>(cacheKey, cancellationToken);
            if (cached != null)
            {
                _logger.LogDebug("热门轴承缓存命中");
                return cached;
            }

            var bearings = await _bearingRepository.GetHotBearingsAsync(request.Count, cancellationToken);

            var result = bearings.Select(b => b.ToDto()).ToList();

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(1), cancellationToken);

            return result;
        }
    }
}
