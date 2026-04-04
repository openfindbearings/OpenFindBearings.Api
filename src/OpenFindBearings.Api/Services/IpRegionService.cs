using Microsoft.Extensions.Caching.Memory;

namespace OpenFindBearings.Api.Services
{
    /// <summary>
    /// IP 地区解析服务接口
    /// </summary>
    public interface IIpRegionService
    {
        /// <summary>
        /// 根据 IP 地址获取所在省份和城市
        /// </summary>
        /// <param name="ip">IP 地址</param>
        /// <returns>省份和城市，解析失败返回 null</returns>
        Task<(string? Province, string? City)?> GetRegionByIpAsync(string ip);
    }

    /// <summary>
    /// IP 地区解析服务实现
    /// 使用 ip-api.com 免费接口，带缓存
    /// </summary>
    public class IpRegionService : IIpRegionService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<IpRegionService> _logger;

        /// <summary>
        /// 缓存过期时间（24小时）
        /// </summary>
        private const int CacheMinutes = 1440;

        public IpRegionService(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            ILogger<IpRegionService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// 根据 IP 地址获取所在省份和城市
        /// </summary>
        public async Task<(string? Province, string? City)?> GetRegionByIpAsync(string ip)
        {
            // 本地 IP 或内网 IP，直接返回"本地"
            if (IsLocalIp(ip))
                return ("本地", "本地");

            // 从缓存获取
            var cacheKey = $"ip_region_{ip}";
            if (_cache.TryGetValue(cacheKey, out (string Province, string City)? cached))
                return cached;

            try
            {
                // 使用 ip-api.com 免费接口（限制 45次/分钟）
                var response = await _httpClient.GetFromJsonAsync<IpInfo>($"http://ip-api.com/json/{ip}");

                if (response?.Status == "success")
                {
                    var result = (response.RegionName, response.City);
                    _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CacheMinutes));
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "IP 地区解析失败: {Ip}", ip);
            }

            return null;
        }

        /// <summary>
        /// 判断是否为本地 IP
        /// </summary>
        private bool IsLocalIp(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return true;
            if (ip == "127.0.0.1" || ip == "::1") return true;
            if (ip.StartsWith("192.168.") || ip.StartsWith("10.") || ip.StartsWith("172.")) return true;
            return false;
        }

        /// <summary>
        /// IP 信息响应模型
        /// </summary>
        private class IpInfo
        {
            public string Status { get; set; } = string.Empty;
            public string Country { get; set; } = string.Empty;
            public string RegionName { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
        }
    }
}
