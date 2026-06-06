using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenFindBearings.Application.Services;

namespace OpenFindBearings.Infrastructure.Services
{
    public class FileStorageSettings
    {
        public string BasePath { get; set; } = "uploads";
        public string BaseUrl { get; set; } = "/uploads";
        public long MaxFileSize { get; set; } = 10 * 1024 * 1024;
        public List<string> AllowedExtensions { get; set; } = new() { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg", ".dwg", ".dxf" };
    }

    public class LocalFileService : IFileService
    {
        private readonly ILogger<LocalFileService> _logger;
        private readonly FileStorageSettings _settings;
        private readonly string _rootPath;

        public LocalFileService(
            ILogger<LocalFileService> logger,
            IOptions<FileStorageSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
            _rootPath = Path.Combine(Directory.GetCurrentDirectory(), _settings.BasePath);

            if (!Directory.Exists(_rootPath))
            {
                Directory.CreateDirectory(_rootPath);
            }
        }

        public async Task<UploadResult> UploadAsync(Stream file, string fileName, string? subDirectory = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                if (!_settings.AllowedExtensions.Contains(extension))
                {
                    return new UploadResult
                    {
                        Success = false,
                        OriginalFileName = fileName,
                        ErrorMessage = $"不允许的文件类型: {extension}"
                    };
                }

                var targetDir = string.IsNullOrEmpty(subDirectory)
                    ? _rootPath
                    : Path.Combine(_rootPath, subDirectory);

                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                var newFileName = $"{Guid.NewGuid()}{extension}";
                var relativePath = string.IsNullOrEmpty(subDirectory) ? newFileName : $"{subDirectory}/{newFileName}";
                var fullPath = Path.Combine(_rootPath, relativePath);

                await using var outputStream = new FileStream(fullPath, FileMode.Create);
                await file.CopyToAsync(outputStream, cancellationToken);

                var fileInfo = new FileInfo(fullPath);

                return new UploadResult
                {
                    Success = true,
                    FileName = newFileName,
                    OriginalFileName = fileName,
                    FileUrl = GetFileUrl(relativePath),
                    FileSize = fileInfo.Length,
                    ContentType = GetContentType(extension)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文件上传失败: {FileName}", fileName);
                return new UploadResult
                {
                    Success = false,
                    OriginalFileName = fileName,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<List<UploadResult>> UploadManyAsync(List<(string FileName, Stream Stream)> files, string? subDirectory = null, CancellationToken cancellationToken = default)
        {
            var results = new List<UploadResult>();
            foreach (var (fileName, stream) in files)
            {
                var result = await UploadAsync(stream, fileName, subDirectory, cancellationToken);
                results.Add(result);
            }
            return results;
        }

        public Task<bool> DeleteAsync(string fileUrl)
        {
            try
            {
                var relativePath = GetRelativePath(fileUrl);
                var fullPath = Path.Combine(_rootPath, relativePath);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("文件已删除: {Path}", fullPath);
                    return Task.FromResult(true);
                }

                _logger.LogWarning("文件不存在: {Path}", fullPath);
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除文件失败: {FileUrl}", fileUrl);
                return Task.FromResult(false);
            }
        }

        public Task<bool> ExistsAsync(string fileUrl)
        {
            try
            {
                var relativePath = GetRelativePath(fileUrl);
                var fullPath = Path.Combine(_rootPath, relativePath);
                return Task.FromResult(File.Exists(fullPath));
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public string GetFileUrl(string relativePath)
        {
            return $"{_settings.BaseUrl}/{relativePath}".Replace("\\", "/");
        }

        private string GetRelativePath(string fileUrl)
        {
            if (fileUrl.StartsWith(_settings.BaseUrl))
            {
                return fileUrl.Substring(_settings.BaseUrl.Length).TrimStart('/');
            }
            return fileUrl.TrimStart('/');
        }

        private static string GetContentType(string extension) => extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".dwg" => "image/vnd.dwg",
            ".dxf" => "image/vnd.dxf",
            _ => "application/octet-stream"
        };
    }
}