using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Mobile.DTOs;
using OpenFindBearings.Application.Features.Mobile.Queries;
using OpenFindBearings.Domain.Interfaces;
using OpenFindBearings.Domain.Specifications;

namespace OpenFindBearings.Application.Features.Mobile.Handlers
{
    /// <summary>
    /// 获取移动端首页查询处理器
    /// </summary>
    public class GetMobileHomeQueryHandler : IRequestHandler<GetMobileHomeQuery, MobileHomeDto>
    {
        private readonly IBearingRepository _bearingRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IUserBearingFavoriteRepository _favoriteRepository;
        private readonly IUserMerchantFollowRepository _followRepository;
        private readonly IUserBearingHistoryRepository _historyRepository;
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly ILogger<GetMobileHomeQueryHandler> _logger;

        public GetMobileHomeQueryHandler(
            IBearingRepository bearingRepository,
            IMerchantRepository merchantRepository,
            IUserBearingFavoriteRepository favoriteRepository,
            IUserMerchantFollowRepository followRepository,
            IUserBearingHistoryRepository historyRepository,
            ISystemConfigRepository systemConfigRepository,
            ILogger<GetMobileHomeQueryHandler> logger)
        {
            _bearingRepository = bearingRepository;
            _merchantRepository = merchantRepository;
            _favoriteRepository = favoriteRepository;
            _followRepository = followRepository;
            _historyRepository = historyRepository;
            _systemConfigRepository = systemConfigRepository;
            _logger = logger;
        }

        public async Task<MobileHomeDto> Handle(
            GetMobileHomeQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取移动端首页数据, IsAuthenticated={IsAuthenticated}", request.IsAuthenticated);

            // 获取配置中的轮播图和分类
            var configs = await _systemConfigRepository.GetAllAsync(cancellationToken);

            // 获取热门轴承
            var hotBearings = await _bearingRepository.GetHotBearingsAsync(6, cancellationToken);

            // 获取推荐商家（已认证、等级高的优先）
            var merchantSearch = new MerchantSearchParams
            {
                VerifiedOnly = true,
                PageSize = 4
            };
            var merchants = await _merchantRepository.SearchAsync(merchantSearch, cancellationToken);

            var result = new MobileHomeDto
            {
                // 轮播图（从配置读取）
                Banners = await GetBannersAsync(configs, cancellationToken),

                // 分类（从配置读取）
                Categories = await GetCategoriesAsync(configs, cancellationToken),

                // 热门轴承
                HotBearings = hotBearings.Select(b => new MobileBearingLightDto
                {
                    Id = b.Id,
                    PartNumber = b.PartNumber,
                    BrandName = b.Brand?.Name ?? string.Empty,
                    InnerDiameter = b.Dimensions.InnerDiameter,
                    OuterDiameter = b.Dimensions.OuterDiameter,
                    Width = b.Dimensions.Width,
                    ThumbnailUrl = GetBearingThumbnailUrl(b.Id, b.Brand?.Code),
                    MinPrice = null // 可以后续计算
                }).ToList(),

                // 推荐商家
                RecommendedMerchants = merchants.Select(m => new MerchantSimpleDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    LogoUrl = GetMerchantLogoUrl(m.Id, m.Name),
                    Grade = m.Grade.ToString(),
                    ProductCount = m.MerchantBearings?.Count ?? 0
                }).ToList()
            };

            // 如果用户已登录，获取用户统计信息
            if (request.IsAuthenticated && request.UserId.HasValue)
            {
                result.UserStats = new UserStatsDto
                {
                    FavoriteCount = await _favoriteRepository.CountByUserIdAsync(request.UserId.Value, cancellationToken),
                    FollowCount = await _followRepository.CountByUserIdAsync(request.UserId.Value, cancellationToken),
                    HistoryCount = await _historyRepository.CountByUserIdAsync(request.UserId.Value, cancellationToken)
                };
            }

            return result;
        }

        /// <summary>
        /// 获取轮播图配置
        /// </summary>
        private async Task<List<BannerDto>> GetBannersAsync(
            List<Domain.Entities.SystemConfig> configs,
            CancellationToken cancellationToken)
        {
            var banners = new List<BannerDto>();

            // 从配置读取轮播图数量
            var bannerCount = configs.FirstOrDefault(c => c.Key == "Mobile.BannerCount")?.Value ?? "3";
            if (!int.TryParse(bannerCount, out var count)) count = 3;

            for (int i = 1; i <= count; i++)
            {
                var title = configs.FirstOrDefault(c => c.Key == $"Mobile.Banner{i}.Title")?.Value ?? $"Banner {i}";
                var imageUrl = configs.FirstOrDefault(c => c.Key == $"Mobile.Banner{i}.ImageUrl")?.Value ?? $"/images/banners/banner{i}.jpg";
                var linkUrl = configs.FirstOrDefault(c => c.Key == $"Mobile.Banner{i}.LinkUrl")?.Value;

                banners.Add(new BannerDto
                {
                    Id = i.ToString(),
                    Title = title,
                    ImageUrl = imageUrl,
                    LinkUrl = linkUrl
                });
            }

            return banners;
        }

        /// <summary>
        /// 获取分类配置
        /// </summary>
        private async Task<List<CategoryDto>> GetCategoriesAsync(
            List<Domain.Entities.SystemConfig> configs,
            CancellationToken cancellationToken)
        {
            var categories = new List<CategoryDto>();

            // 从配置读取分类
            var categoryIds = configs.FirstOrDefault(c => c.Key == "Mobile.Categories")?.Value ?? "dgb,acb,trb,srb";
            var ids = categoryIds.Split(',');

            foreach (var id in ids)
            {
                var name = configs.FirstOrDefault(c => c.Key == $"Mobile.Category.{id}.Name")?.Value ?? GetDefaultCategoryName(id);
                var iconUrl = configs.FirstOrDefault(c => c.Key == $"Mobile.Category.{id}.IconUrl")?.Value ?? $"/images/categories/{id}.png";

                categories.Add(new CategoryDto
                {
                    Id = id,
                    Name = name,
                    IconUrl = iconUrl
                });
            }

            return categories;
        }

        private string GetDefaultCategoryName(string id)
        {
            return id.ToLower() switch
            {
                "dgb" => "深沟球",
                "acb" => "角接触",
                "trb" => "圆锥滚子",
                "srb" => "调心滚子",
                _ => id
            };
        }

        private string GetBearingThumbnailUrl(Guid bearingId, string? brandCode)
        {
            // 可以根据品牌代码返回不同的默认图
            return $"/images/bearings/{bearingId}.jpg";
        }

        private string GetMerchantLogoUrl(Guid merchantId, string merchantName)
        {
            // 返回商家Logo，如果没有则返回默认图
            return $"/images/merchants/{merchantId}.jpg";
        }
    }
}
