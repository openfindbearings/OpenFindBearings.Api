using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
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

            var result = bearings.Select(b => new BearingDto
            {
                Id = b.Id,
                CurrentCode = b.CurrentCode,
                FormerCode = b.FormerCode,                 // ✅ 新增
                Name = b.Name,
                Description = b.Description,
                InnerDiameter = b.Dimensions.InnerDiameter,
                OuterDiameter = b.Dimensions.OuterDiameter,
                Width = b.Dimensions.Width,
                Weight = b.Weight,
                BrandId = b.BrandId,
                BrandName = b.Brand?.Name ?? string.Empty,
                BearingTypeId = b.BearingTypeId,
                BearingTypeName = b.BearingType,
                ViewCount = b.ViewCount,
                FavoriteCount = b.FavoriteCount,
                OriginCountry = b.OriginCountry,
                Category = b.Category.ToString(),
                IsStandard = b.IsStandard                  // ✅ 新增
            }).ToList();

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(1), cancellationToken);

            return result;
        }
    }
}
