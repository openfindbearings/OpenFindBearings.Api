using Microsoft.Extensions.Configuration;
using OpenFindBearings.Application.Services;

namespace OpenFindBearings.Infrastructure.Services.ObjectStorage
{
    /// <summary>
    /// 本地盘对象存储实现（开发环境默认）：以 FileStorage:LocalPath 为根目录写文件，
    /// URL 保持 "/{key}" 相对键形态，与 MinioStorage 产出一致，业务层无感切换。
    /// </summary>
    public class LocalFileStorage : IObjectStorageService
    {
        private readonly string _root;
        private readonly string _cdnDomain;

        /// <summary>从配置读取本地根目录（默认 wwwroot，生产由 media 服务同源直出的目录）</summary>
        public LocalFileStorage(IConfiguration configuration)
        {
            _root = configuration["FileStorage:LocalPath"] ?? "wwwroot";
            _cdnDomain = (configuration["FileStorage:CdnDomain"] ?? "").TrimEnd('/');
        }

        /// <inheritdoc />
        public async Task<string> UploadAsync(string key, byte[] content, string contentType, CancellationToken ct = default)
        {
            var fullPath = Path.Combine(_root, key);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllBytesAsync(fullPath, content, ct);
            return GetUrl(key);
        }

        /// <inheritdoc />
        public Task<bool> DeleteAsync(string key, CancellationToken ct = default)
        {
            var fullPath = Path.Combine(_root, key);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        /// <inheritdoc />
        public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        {
            return Task.FromResult(File.Exists(Path.Combine(_root, key)));
        }

        /// <inheritdoc />
        public string GetUrl(string key)
        {
            var clean = key.TrimStart('/');
            return string.IsNullOrEmpty(_cdnDomain) ? $"/{clean}" : $"{_cdnDomain}/{clean}";
        }
    }
}
