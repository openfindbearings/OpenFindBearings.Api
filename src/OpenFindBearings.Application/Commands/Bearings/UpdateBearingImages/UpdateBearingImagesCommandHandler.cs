using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Application.Commands.Bearings.UpdateBearingImages
{
    /// <summary>
    /// 更新轴承图片命令处理器（v2.21.3）：校验文件格式与大小后上传到对象存储，
    /// 只替换指定槽位并保留另一张；写入后标记为人工来源，避免后续互联网同步覆盖。
    /// </summary>
    public class UpdateBearingImagesCommandHandler : IRequestHandler<UpdateBearingImagesCommand, UpdateBearingImagesResult>
    {
        /// <summary>单张图片大小上限（5MB）：型号主图无需更大，防止占用存储与内存</summary>
        private const long MaxFileBytes = 5 * 1024 * 1024;

        /// <summary>允许的图片扩展名（小写含点）</summary>
        private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

        private readonly IBearingRepository _bearingRepository;
        private readonly IObjectStorageService _storage;
        private readonly ILogger<UpdateBearingImagesCommandHandler> _logger;

        /// <summary>
        /// 构造函数：注入轴承仓储、对象存储与日志
        /// </summary>
        public UpdateBearingImagesCommandHandler(
            IBearingRepository bearingRepository,
            IObjectStorageService storage,
            ILogger<UpdateBearingImagesCommandHandler> logger)
        {
            _bearingRepository = bearingRepository;
            _storage = storage;
            _logger = logger;
        }

        /// <summary>
        /// 执行图片更新：先校验变更请求，再按槽位上传/清空，最后一次性写回两个槽位
        /// </summary>
        public async Task<UpdateBearingImagesResult> Handle(UpdateBearingImagesCommand request, CancellationToken cancellationToken)
        {
            var bearing = await _bearingRepository.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"轴承不存在: {request.Id}");

            var hasImage3D = request.Image3D is { Length: > 0 };
            var hasImage2D = request.Image2D is { Length: > 0 };
            if (!hasImage3D && !hasImage2D && !request.ClearImage3D && !request.ClearImage2D)
                throw new InvalidOperationException("未提供图片变更，请选择文件或勾选清空");

            // 文件优先于清空标记：同时提交时以新文件为准，避免"传了图又被清空"的歧义
            var image3DUrl = hasImage3D
                ? await UploadAsync(request.Id, "3d", request.Image3D!, request.Image3DExtension, cancellationToken)
                : (request.ClearImage3D ? null : bearing.Image3DUrl);

            var image2DUrl = hasImage2D
                ? await UploadAsync(request.Id, "2d", request.Image2D!, request.Image2DExtension, cancellationToken)
                : (request.ClearImage2D ? null : bearing.Image2DUrl);

            bearing.SetImages(image3DUrl, image2DUrl);
            // 人工维护标记：与文本纠错审批同口径，防止互联网同步把运营改过的图刷回去
            bearing.SetDataSource(DataSource.FromManual());
            await _bearingRepository.UpdateAsync(bearing, cancellationToken);

            _logger.LogInformation("轴承图片已更新: BearingId={BearingId}, Image3D={Has3D}, Image2D={Has2D}",
                request.Id, image3DUrl != null, image2DUrl != null);

            return new UpdateBearingImagesResult(image3DUrl, image2DUrl);
        }

        /// <summary>
        /// 校验并上传单张图片，返回可访问 URL；格式或大小不合规时抛业务异常（映射为 400）
        /// </summary>
        /// <param name="bearingId">轴承ID，用于组织存储路径</param>
        /// <param name="slot">槽位标识（3d/2d）</param>
        /// <param name="content">文件字节</param>
        /// <param name="extension">文件扩展名（含点）</param>
        /// <param name="ct">取消令牌</param>
        private async Task<string> UploadAsync(Guid bearingId, string slot, byte[] content, string? extension, CancellationToken ct)
        {
            var ext = (extension ?? string.Empty).Trim().ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                throw new InvalidOperationException("仅支持 JPG、PNG、WEBP 图片格式");

            if (content.Length > MaxFileBytes)
                throw new InvalidOperationException("图片大小不能超过 5MB");

            var contentType = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            // 时间戳保证同一槽位重复替换时 key 不冲突（旧文件保留，避免误删被其他记录引用的图）
            var key = $"uploads/bearings/{bearingId:N}/{slot}_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
            return await _storage.UploadAsync(key, content, contentType, ct);
        }
    }
}
