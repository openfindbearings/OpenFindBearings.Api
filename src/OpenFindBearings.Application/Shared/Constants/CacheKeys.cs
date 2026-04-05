using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OpenFindBearings.Application.Shared.Constants
{
    /// <summary>
    /// 缓存键常量定义
    /// 集中管理所有缓存键，避免硬编码
    /// 所有缓存键格式：{module}:{entity}:{identifier}:{suffix}
    /// </summary>
    public static class CacheKeys
    {
        // ============ 轴承相关 (Bearing) ============

        /// <summary>
        /// 单个轴承缓存键
        /// 格式: bearing:{id}
        /// </summary>
        public const string Bearing = "bearing:{0}";

        /// <summary>
        /// 轴承型号查询缓存键
        /// 格式: bearing:part:{partNumber}
        /// </summary>
        public const string BearingByPartNumber = "bearing:part:{0}";

        /// <summary>
        /// 热门轴承列表缓存键
        /// 格式: bearing:hot:{count}
        /// </summary>
        public const string HotBearings = "bearing:hot:{0}";

        /// <summary>
        /// 轴承替代品列表缓存键
        /// 格式: bearing:{bearingId}:interchanges
        /// </summary>
        public const string BearingInterchanges = "bearing:{0}:interchanges";

        /// <summary>
        /// 轴承的在售商家列表缓存键
        /// 格式: bearing:{bearingId}:merchants
        /// </summary>
        public const string BearingMerchants = "bearing:{0}:merchants";

        /// <summary>
        /// 最近添加的轴承列表缓存键
        /// 格式: bearing:recent
        /// </summary>
        public const string RecentBearings = "bearing:recent";

        /// <summary>
        /// 轴承是否被收藏缓存键
        /// 格式: bearing:{bearingId}:favorited
        /// </summary>
        public const string BearingFavorited = "bearing:{0}:favorited";

        // ============ 商家相关 (Merchant) ============

        /// <summary>
        /// 单个商家缓存键
        /// 格式: merchant:{id}
        /// </summary>
        public const string Merchant = "merchant:{0}";

        /// <summary>
        /// 商家产品列表缓存键
        /// 格式: merchant:{merchantId}:products
        /// </summary>
        public const string MerchantProducts = "merchant:{0}:products";

        /// <summary>
        /// 商家在售产品列表缓存键
        /// 格式: merchant:{merchantId}:onsale
        /// </summary>
        public const string MerchantOnSaleProducts = "merchant:{0}:onsale";

        /// <summary>
        /// 商家粉丝数量缓存键
        /// 格式: merchant:{merchantId}:followers
        /// </summary>
        public const string MerchantFollowers = "merchant:{0}:followers";

        /// <summary>
        /// 商家粉丝列表缓存键
        /// 格式: merchant:{merchantId}:followerlist
        /// </summary>
        public const string MerchantFollowerList = "merchant:{0}:followerlist";

        // ============ 用户相关 (User) ============

        /// <summary>
        /// 单个用户缓存键（通过AuthUserId）
        /// 格式: user:auth:{authUserId}
        /// </summary>
        public const string UserByAuthId = "user:auth:{0}";

        /// <summary>
        /// 单个用户缓存键（通过Id）
        /// 格式: user:id:{userId}
        /// </summary>
        public const string UserById = "user:id:{0}";

        /// <summary>
        /// 用户收藏列表缓存键
        /// 格式: user:{userId}:favorites
        /// </summary>
        public const string UserFavorites = "user:{0}:favorites";

        /// <summary>
        /// 用户收藏轴承ID列表缓存键
        /// 格式: user:{userId}:favoriteids
        /// </summary>
        public const string UserFavoriteIds = "user:{0}:favoriteids";

        /// <summary>
        /// 用户关注列表缓存键
        /// 格式: user:{userId}:follows
        /// </summary>
        public const string UserFollows = "user:{0}:follows";

        /// <summary>
        /// 用户关注商家ID列表缓存键
        /// 格式: user:{userId}:followids
        /// </summary>
        public const string UserFollowIds = "user:{0}:followids";

        /// <summary>
        /// 用户权限列表缓存键
        /// 格式: user:{userId}:permissions
        /// </summary>
        public const string UserPermissions = "user:{0}:permissions";

        /// <summary>
        /// 用户角色列表缓存键
        /// 格式: user:{userId}:roles
        /// </summary>
        public const string UserRoles = "user:{0}:roles";

        /// <summary>
        /// 用户轴承浏览历史列表缓存键
        /// 格式: user:{userId}:bearinghistory
        /// </summary>
        public const string UserBearingHistory = "user:{0}:bearinghistory";

        /// <summary>
        /// 用户商家历史列表缓存键
        /// 格式: user:{userId}:merchanthistory
        /// </summary>
        public const string UserMerchantHistory = "user:{0}:merchanthistory";

        // ============ 品牌和类型相关 (Brand & Type) ============

        /// <summary>
        /// 所有品牌列表缓存键
        /// </summary>
        public const string AllBrands = "brand:all";

        /// <summary>
        /// 单个品牌缓存键
        /// 格式: brand:{id}
        /// </summary>
        public const string Brand = "brand:{0}";

        /// <summary>
        /// 品牌下的轴承列表缓存键
        /// 格式: brand:{brandId}:bearings
        /// </summary>
        public const string BrandBearings = "brand:{0}:bearings";

        /// <summary>
        /// 品牌下的轴承数量缓存键
        /// 格式: brand:{brandId}:bearingcount
        /// </summary>
        public const string BrandBearingCount = "brand:{0}:bearingcount";

        /// <summary>
        /// 所有轴承类型列表缓存键
        /// </summary>
        public const string AllBearingTypes = "bearingtype:all";

        /// <summary>
        /// 单个轴承类型缓存键
        /// 格式: bearingtype:{id}
        /// </summary>
        public const string BearingType = "bearingtype:{0}";

        /// <summary>
        /// 类型下的轴承列表缓存键
        /// 格式: bearingtype:{typeId}:bearings
        /// </summary>
        public const string BearingTypeBearings = "bearingtype:{0}:bearings";

        /// <summary>
        /// 类型下的轴承数量缓存键
        /// 格式: bearingtype:{typeId}:bearingcount
        /// </summary>
        public const string BearingTypeCount = "bearingtype:{0}:bearingcount";

        // ============ 统计相关 (Statistics) ============

        /// <summary>
        /// 全局统计信息缓存键
        /// </summary>
        public const string GlobalStatistics = "stat:global";

        /// <summary>
        /// 品牌统计信息缓存键
        /// 格式: stat:brand:{brandId}
        /// </summary>
        public const string BrandStatistics = "stat:brand:{0}";

        /// <summary>
        /// 仪表盘统计数据缓存键
        /// </summary>
        public const string DashboardStats = "stat:dashboard";

        // ============ 搜索相关 (Search) ============

        /// <summary>
        /// 轴承搜索结果缓存键
        /// 格式: search:bearings:{hash}
        /// 使用搜索参数的哈希值作为键
        /// </summary>
        public const string SearchBearings = "search:bearings:{0}";

        /// <summary>
        /// 商家搜索结果缓存键
        /// 格式: search:merchants:{hash}
        /// </summary>
        public const string SearchMerchants = "search:merchants:{0}";

        // ============ 同步任务相关 (Sync) ============

        /// <summary>
        /// 同步任务状态缓存键
        /// 格式: sync:task:{taskId}
        /// </summary>
        public const string SyncTaskStatus = "sync:task:{0}";

        // ============ 辅助方法 ============

        /// <summary>
        /// 格式化缓存键
        /// </summary>
        /// <param name="key">缓存键模板</param>
        /// <param name="args">格式化参数</param>
        /// <returns>格式化后的缓存键</returns>
        public static string Format(string key, params object[] args)
        {
            return string.Format(key, args);
        }

        /// <summary>
        /// 生成搜索参数的哈希值
        /// 用于创建唯一的搜索缓存键
        /// </summary>
        /// <param name="parameters">搜索参数对象</param>
        /// <returns>哈希字符串（Base64格式，取前16位）</returns>
        public static string GetSearchHash(object parameters)
        {
            if (parameters == null)
                return "null";

            try
            {
                var json = JsonSerializer.Serialize(parameters);
                using var sha = SHA256.Create();
                var bytes = Encoding.UTF8.GetBytes(json);
                var hash = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash)[..16]; // 取前16位作为短哈希
            }
            catch (Exception)
            {
                // 如果序列化失败，返回一个基于时间的简单哈希
                return Guid.NewGuid().ToString()[..8];
            }
        }

        // ============ 便捷方法 - 轴承相关 ============

        /// <summary>
        /// 获取单个轴承缓存键
        /// </summary>
        public static string GetBearingKey(Guid bearingId) => Format(Bearing, bearingId);

        /// <summary>
        /// 获取轴承型号缓存键
        /// </summary>
        public static string GetBearingByPartNumberKey(string partNumber) =>
            Format(BearingByPartNumber, partNumber);

        /// <summary>
        /// 获取热门轴承缓存键
        /// </summary>
        public static string GetHotBearingsKey(int count) => Format(HotBearings, count);

        /// <summary>
        /// 获取轴承替代品列表缓存键
        /// </summary>
        public static string GetBearingInterchangesKey(Guid bearingId) =>
            Format(BearingInterchanges, bearingId);

        /// <summary>
        /// 获取轴承在售商家列表缓存键
        /// </summary>
        public static string GetBearingMerchantsKey(Guid bearingId) =>
            Format(BearingMerchants, bearingId);

        /// <summary>
        /// 获取最近添加轴承列表缓存键
        /// </summary>
        public static string GetRecentBearingsKey() => RecentBearings;

        /// <summary>
        /// 获取轴承收藏状态缓存键
        /// </summary>
        public static string GetBearingFavoritedKey(Guid bearingId) =>
            Format(BearingFavorited, bearingId);

        // ============ 便捷方法 - 商家相关 ============

        /// <summary>
        /// 获取单个商家缓存键
        /// </summary>
        public static string GetMerchantKey(Guid merchantId) => Format(Merchant, merchantId);

        /// <summary>
        /// 获取商家产品列表缓存键
        /// </summary>
        public static string GetMerchantProductsKey(Guid merchantId) =>
            Format(MerchantProducts, merchantId);

        /// <summary>
        /// 获取商家在售产品列表缓存键
        /// </summary>
        public static string GetMerchantOnSaleProductsKey(Guid merchantId) =>
            Format(MerchantOnSaleProducts, merchantId);

        /// <summary>
        /// 获取商家粉丝数量缓存键
        /// </summary>
        public static string GetMerchantFollowersKey(Guid merchantId) =>
            Format(MerchantFollowers, merchantId);

        /// <summary>
        /// 获取商家粉丝列表缓存键
        /// </summary>
        public static string GetMerchantFollowerListKey(Guid merchantId) =>
            Format(MerchantFollowerList, merchantId);

        // ============ 便捷方法 - 用户相关 ============

        /// <summary>
        /// 获取用户缓存键（通过AuthUserId）
        /// </summary>
        public static string GetUserByAuthIdKey(string authUserId) =>
            Format(UserByAuthId, authUserId);

        /// <summary>
        /// 获取用户缓存键（通过Id）
        /// </summary>
        public static string GetUserByIdKey(Guid userId) => Format(UserById, userId);

        /// <summary>
        /// 获取用户收藏列表缓存键
        /// </summary>
        public static string GetUserFavoritesKey(Guid userId) => Format(UserFavorites, userId);

        /// <summary>
        /// 获取用户收藏轴承ID列表缓存键
        /// </summary>
        public static string GetUserFavoriteIdsKey(Guid userId) => Format(UserFavoriteIds, userId);

        /// <summary>
        /// 获取用户关注列表缓存键
        /// </summary>
        public static string GetUserFollowsKey(Guid userId) => Format(UserFollows, userId);

        /// <summary>
        /// 获取用户关注商家ID列表缓存键
        /// </summary>
        public static string GetUserFollowIdsKey(Guid userId) => Format(UserFollowIds, userId);

        /// <summary>
        /// 获取用户权限列表缓存键
        /// </summary>
        public static string GetUserPermissionsKey(Guid userId) => Format(UserPermissions, userId);

        /// <summary>
        /// 获取用户角色列表缓存键
        /// </summary>
        public static string GetUserRolesKey(Guid userId) => Format(UserRoles, userId);

        /// <summary>
        /// 获取用户轴承浏览历史缓存键
        /// </summary>
        public static string GetUserBearingHistoryKey(Guid userId) =>
            Format(UserBearingHistory, userId);

        /// <summary>
        /// 获取用户商家浏览历史缓存键
        /// </summary>
        public static string GetUserMerchantHistoryKey(Guid userId) =>
            Format(UserMerchantHistory, userId);

        // ============ 便捷方法 - 品牌和类型 ============

        /// <summary>
        /// 获取所有品牌列表缓存键
        /// </summary>
        public static string GetAllBrandsKey() => AllBrands;

        /// <summary>
        /// 获取单个品牌缓存键
        /// </summary>
        public static string GetBrandKey(Guid brandId) => Format(Brand, brandId);

        /// <summary>
        /// 获取品牌下的轴承列表缓存键
        /// </summary>
        public static string GetBrandBearingsKey(Guid brandId) => Format(BrandBearings, brandId);

        /// <summary>
        /// 获取品牌下的轴承数量缓存键
        /// </summary>
        public static string GetBrandBearingCountKey(Guid brandId) =>
            Format(BrandBearingCount, brandId);

        /// <summary>
        /// 获取所有轴承类型列表缓存键
        /// </summary>
        public static string GetAllBearingTypesKey() => AllBearingTypes;

        /// <summary>
        /// 获取单个轴承类型缓存键
        /// </summary>
        public static string GetBearingTypeKey(Guid typeId) => Format(BearingType, typeId);

        /// <summary>
        /// 获取类型下的轴承列表缓存键
        /// </summary>
        public static string GetBearingTypeBearingsKey(Guid typeId) =>
            Format(BearingTypeBearings, typeId);

        /// <summary>
        /// 获取类型下的轴承数量缓存键
        /// </summary>
        public static string GetBearingTypeCountKey(Guid typeId) =>
            Format(BearingTypeCount, typeId);

        // ============ 便捷方法 - 统计 ============

        /// <summary>
        /// 获取全局统计缓存键
        /// </summary>
        public static string GetGlobalStatisticsKey() => GlobalStatistics;

        /// <summary>
        /// 获取品牌统计缓存键
        /// </summary>
        public static string GetBrandStatisticsKey(Guid brandId) => Format(BrandStatistics, brandId);

        /// <summary>
        /// 获取仪表盘统计缓存键
        /// </summary>
        public static string GetDashboardStatsKey() => DashboardStats;

        // ============ 便捷方法 - 搜索 ============

        /// <summary>
        /// 获取轴承搜索结果缓存键
        /// </summary>
        /// <param name="searchParams">搜索参数对象</param>
        public static string GetSearchBearingsKey(object searchParams) =>
            Format(SearchBearings, GetSearchHash(searchParams));

        /// <summary>
        /// 获取商家搜索结果缓存键
        /// </summary>
        /// <param name="searchParams">搜索参数对象</param>
        public static string GetSearchMerchantsKey(object searchParams) =>
            Format(SearchMerchants, GetSearchHash(searchParams));

        // ============ 便捷方法 - 同步任务 ============

        /// <summary>
        /// 获取同步任务状态缓存键
        /// </summary>
        public static string GetSyncTaskKey(Guid taskId) => Format(SyncTaskStatus, taskId);

        // ============ 缓存键模式匹配 ============

        /// <summary>
        /// 缓存键模式匹配
        /// 用于批量删除操作
        /// </summary>
        public static class Patterns
        {
            /// <summary>
            /// 匹配所有轴承相关缓存
            /// </summary>
            public const string AllBearings = "bearing:*";

            /// <summary>
            /// 匹配特定轴承的所有相关缓存
            /// </summary>
            public static string ForBearing(Guid bearingId) => $"bearing:{bearingId}:*";

            /// <summary>
            /// 匹配所有商家相关缓存
            /// </summary>
            public const string AllMerchants = "merchant:*";

            /// <summary>
            /// 匹配特定商家的所有相关缓存
            /// </summary>
            public static string ForMerchant(Guid merchantId) => $"merchant:{merchantId}:*";

            /// <summary>
            /// 匹配所有用户相关缓存
            /// </summary>
            public const string AllUsers = "user:*";

            /// <summary>
            /// 匹配特定用户的所有相关缓存
            /// </summary>
            public static string ForUser(Guid userId) => $"user:{userId}:*";

            /// <summary>
            /// 匹配所有品牌相关缓存
            /// </summary>
            public const string AllBrands = "brand:*";

            /// <summary>
            /// 匹配特定品牌的所有相关缓存
            /// </summary>
            public static string ForBrand(Guid brandId) => $"brand:{brandId}:*";

            /// <summary>
            /// 匹配所有类型相关缓存
            /// </summary>
            public const string AllBearingTypes = "bearingtype:*";

            /// <summary>
            /// 匹配特定类型的所有相关缓存
            /// </summary>
            public static string ForBearingType(Guid typeId) => $"bearingtype:{typeId}:*";

            /// <summary>
            /// 匹配所有搜索结果缓存
            /// </summary>
            public const string AllSearchResults = "search:*";

            /// <summary>
            /// 匹配所有统计缓存
            /// </summary>
            public const string AllStatistics = "stat:*";
        }
    }
}
