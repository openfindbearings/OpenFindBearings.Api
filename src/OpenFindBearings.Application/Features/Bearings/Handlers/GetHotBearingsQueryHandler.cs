using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Constants;
using OpenFindBearings.Application.Features.Bearings.DTOs;
using OpenFindBearings.Application.Features.Bearings.Queries;
using OpenFindBearings.Application.Interfaces;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Bearings.Handlers
{
    /// <summary>
    /// 获取热门轴承查询处理器
    /// </summary>
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
            //var cacheKey = $"hot_bearings_{request.Count}";
            var cacheKey = CacheKeys.GetHotBearingsKey(request.Count);

            // 尝试从缓存获取
            var cached = await _cacheService.GetAsync<List<BearingDto>>(cacheKey, cancellationToken);
            if (cached != null)
            {
                _logger.LogDebug("热门轴承缓存命中");
                return cached;
            }

            // 从数据库获取
            var bearings = await _bearingRepository.GetHotBearingsAsync(request.Count, cancellationToken);

            var result = bearings.Select(b => new BearingDto
            {
                Id = b.Id,
                PartNumber = b.PartNumber,
                Name = b.Name,
                Description = b.Description,
                InnerDiameter = b.Dimensions.InnerDiameter,
                OuterDiameter = b.Dimensions.OuterDiameter,
                Width = b.Dimensions.Width,
                Weight = b.Weight,
                BrandId = b.BrandId,
                BrandName = b.Brand?.Name ?? string.Empty,
                BearingTypeId = b.BearingTypeId,
                BearingTypeName = b.BearingType?.Name ?? string.Empty,
                ViewCount = b.ViewCount,
                FavoriteCount = b.FavoriteCount,
                OriginCountry = b.OriginCountry,
                Category = b.Category.ToString()
            }).ToList();

            // 存入缓存（1小时过期）
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(1), cancellationToken);

            return result;
        }
    }
}
