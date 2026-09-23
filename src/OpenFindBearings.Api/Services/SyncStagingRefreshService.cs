using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace OpenFindBearings.Api.Services
{
    /// <summary>
    /// Sync staging 刷新唤醒服务（v2.17.0）：商户解除归属回公海后，把 Sync staging 库中该商户的
    /// Synced 终态行重置为 Pending 并删除 L 阶段幂等键，使下轮爬取数据能经 E→T→L→API 覆盖通道
    /// 刷新公海商户（"存在性由爬虫管线裁判"的唤醒开关）。best-effort：失败仅日志，可手动补调
    /// （Sync 端点幂等），不回滚已完成的关店事务。
    /// </summary>
    public interface ISyncStagingRefreshService
    {
        /// <summary>
        /// 按商户名唤醒 staging 刷新。返回是否成功（false=Sync 不可达/无匹配行，调用方记审计）
        /// </summary>
        Task<bool> RefreshMerchantAsync(string merchantName, CancellationToken cancellationToken = default);
    }

    /// <inheritdoc/>
    public class SyncStagingRefreshService : ISyncStagingRefreshService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SyncStagingRefreshService> _logger;

        public SyncStagingRefreshService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<SyncStagingRefreshService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<bool> RefreshMerchantAsync(string merchantName, CancellationToken cancellationToken = default)
        {
            try
            {
                var baseUrl = _configuration["Sync:BaseUrl"] ?? "http://openfindbearings-sync:80";
                var token = await GetSyncTokenAsync(cancellationToken);

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{baseUrl}/api/staging/merchants/refresh?name={Uri.EscapeDataString(merchantName)}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var client = _httpClientFactory.CreateClient();
                var response = await client.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Sync staging 刷新失败: Name={Name} Status={Status} Body={Body}",
                        merchantName, response.StatusCode, body);
                    return false;
                }

                _logger.LogInformation("Sync staging 刷新已触发: Name={Name} Body={Body}", merchantName, body);
                return true;
            }
            catch (Exception ex)
            {
                // best-effort：关店已成功，刷新失败仅意味着公海商户数据暂冻结，手动补调即可
                _logger.LogWarning(ex, "Sync staging 刷新异常（可手动补调）: Name={Name}", merchantName);
                return false;
            }
        }

        /// <summary>
        /// 用 sync-client 客户端凭据换取 api:sync scope token（与 SyncInventoryService 同模式）
        /// </summary>
        private async Task<string?> GetSyncTokenAsync(CancellationToken cancellationToken)
        {
            var authority = _configuration["Authentication:Authority"] ?? "https://localhost:7201";
            var client = _httpClientFactory.CreateClient();

            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _configuration["Sync:ClientId"] ?? "sync-client",
                ["client_secret"] = _configuration["Sync:ClientSecret"] ?? string.Empty,
                ["scope"] = _configuration["Sync:Scope"] ?? "api:sync"
            });

            var response = await client.PostAsync($"{authority}/connect/token", form, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("获取 Sync token 失败: Status={Status}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("access_token", out var token)
                ? token.GetString()
                : null;
        }
    }
}
