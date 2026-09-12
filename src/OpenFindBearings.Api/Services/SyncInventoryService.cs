using System.Net.Http.Headers;
using System.Text.Json;

namespace OpenFindBearings.Api.Services
{
    /// <summary>
    /// Sync 库存导入服务接口
    /// </summary>
    public interface ISyncInventoryService
    {
        /// <summary>
        /// 调用 Sync /api/inventory/import 导入商户在售商品 Excel
        /// </summary>
        Task<InventoryImportResult> ImportInventoryAsync(Guid merchantId, Stream fileStream, string fileName, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 库存导入结果
    /// </summary>
    public record InventoryImportResult(bool Success, string Message);

    /// <summary>
    /// Sync 库存导入服务实现
    /// 用 sync-client 客户端凭据换取 api:sync token，转发 Excel 到 Sync /api/inventory/import
    /// （Sync 已有完整的 Excel 解析与型号解析能力，API 侧只做鉴权与转发）
    /// </summary>
    public class SyncInventoryService : ISyncInventoryService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SyncInventoryService> _logger;

        public SyncInventoryService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<SyncInventoryService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<InventoryImportResult> ImportInventoryAsync(
            Guid merchantId,
            Stream fileStream,
            string fileName,
            CancellationToken cancellationToken = default)
        {
            var baseUrl = _configuration["Sync:BaseUrl"] ?? "http://openfindbearings-sync:80";
            var token = await GetSyncTokenAsync(cancellationToken);

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/inventory/import?merchantId={merchantId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(fileStream), "file", fileName);
            request.Content = content;

            var client = _httpClientFactory.CreateClient();
            var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Sync 库存导入失败: Status={Status}, Body={Body}", response.StatusCode, body);
                return new InventoryImportResult(false, $"Sync 返回 {response.StatusCode}");
            }

            return new InventoryImportResult(true, body);
        }

        /// <summary>
        /// 用 sync-client 客户端凭据换取 api:sync scope token
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
                throw new InvalidOperationException("获取 Sync 服务访问令牌失败");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("access_token", out var token)
                ? token.GetString()
                : null;
        }
    }
}
