using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Services;

namespace OpenFindBearings.Infrastructure.Services.ObjectStorage
{
    /// <summary>
    /// MinIO 对象存储实现（S3 标准协议，AWS SDK for .NET 客户端——MinIO 官方推荐路径，
    /// 切换云厂商 OSS/S3 仅改 Endpoint 与凭据）。key 原样入库（uploads/... 前缀），
    /// URL 与 LocalFileStorage 形态一致（/{key}），由 media 服务同源代理 bucket 直出。
    /// </summary>
    public class MinioStorage : IObjectStorageService
    {
        private readonly IAmazonS3 _s3;
        private readonly string _bucket;
        private readonly string _cdnDomain;
        private readonly ILogger<MinioStorage> _logger;

        /// <summary>注入单例 S3 客户端与桶/域名配置</summary>
        public MinioStorage(IAmazonS3 s3, IConfiguration configuration, ILogger<MinioStorage> logger)
        {
            _s3 = s3;
            _logger = logger;
            _bucket = configuration["FileStorage:BucketName"] ?? "media";
            _cdnDomain = (configuration["FileStorage:CdnDomain"] ?? "").TrimEnd('/');
        }

        /// <inheritdoc />
        public async Task<string> UploadAsync(string key, byte[] content, string contentType, CancellationToken ct = default)
        {
            using var stream = new MemoryStream(content);
            await _s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = stream,
                ContentType = contentType
            }, ct);
            _logger.LogDebug("S3 上传成功: {Key} ({Size} bytes)", key, content.Length);
            return GetUrl(key);
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(string key, CancellationToken ct = default)
        {
            try
            {
                await _s3.DeleteObjectAsync(new DeleteObjectRequest { BucketName = _bucket, Key = key }, ct);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "S3 删除失败: {Key}", key);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        {
            try
            {
                await _s3.GetObjectMetadataAsync(_bucket, key, ct);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "S3 存在性检查失败: {Key}", key);
                return false;
            }
        }

        /// <inheritdoc />
        public string GetUrl(string key)
        {
            var clean = key.TrimStart('/');
            return string.IsNullOrEmpty(_cdnDomain) ? $"/{clean}" : $"{_cdnDomain}/{clean}";
        }
    }
}
