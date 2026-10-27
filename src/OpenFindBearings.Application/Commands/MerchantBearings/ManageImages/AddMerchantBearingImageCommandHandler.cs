using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.MerchantBearings.ManageImages
{
    /// <summary>
    /// 追加商家商品实拍图处理器（v2.22.0）：校验商户成员身份与文件规格后上传对象存储，
    /// 写入商家商品自己的图片列表（不影响轴承主数据图）；已通过审核的商品自动重新送审。
    /// </summary>
    public class AddMerchantBearingImageCommandHandler : IRequestHandler<AddMerchantBearingImageCommand, string[]>
    {
        /// <summary>单张图片大小上限（5MB），与后台型号主图口径一致</summary>
        private const long MaxFileBytes = 5 * 1024 * 1024;

        /// <summary>允许的图片扩展名（小写含点）</summary>
        private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly IObjectStorageService _storage;
        private readonly ILogger<AddMerchantBearingImageCommandHandler> _logger;

        /// <summary>
        /// 构造函数：注入商家商品仓储、成员仓储、对象存储与日志
        /// </summary>
        public AddMerchantBearingImageCommandHandler(
            IMerchantBearingRepository merchantBearingRepository,
            IMerchantMemberRepository merchantMemberRepository,
            IObjectStorageService storage,
            ILogger<AddMerchantBearingImageCommandHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _merchantMemberRepository = merchantMemberRepository;
            _storage = storage;
            _logger = logger;
        }

        /// <summary>
        /// 执行追加：成员校验 -> 文件校验 -> 上传 -> 追加图片 -> 必要时重新送审
        /// </summary>
        public async Task<string[]> Handle(AddMerchantBearingImageCommand request, CancellationToken cancellationToken)
        {
            var merchantBearing = await _merchantBearingRepository.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"商家-轴承关联不存在: {request.Id}");

            // 权限：只有该商户在职成员可以维护商品资料（与编辑价格/库存同口径）
            var member = await _merchantMemberRepository.GetActiveByUserAndMerchantAsync(
                request.UserId, merchantBearing.MerchantId, cancellationToken);
            if (member == null)
                throw new UnauthorizedAccessException("无权限修改该商家的商品信息");

            if (merchantBearing.ImageUrls.Length >= Domain.Entities.MerchantBearing.MaxImageCount)
                throw new InvalidOperationException($"最多只能上传 {Domain.Entities.MerchantBearing.MaxImageCount} 张商品图片");

            var ext = (request.Extension ?? string.Empty).Trim().ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                throw new InvalidOperationException("仅支持 JPG、PNG、WEBP 图片格式");
            if (request.Content.Length == 0)
                throw new InvalidOperationException("图片内容为空");
            if (request.Content.Length > MaxFileBytes)
                throw new InvalidOperationException("图片大小不能超过 5MB");

            var contentType = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            // 毫秒级时间戳保证同一商品连续上传不覆盖（对象存储 key 唯一）
            var key = $"uploads/merchant-bearings/{merchantBearing.Id:N}/{DateTime.UtcNow:yyyyMMddHHmmssfff}{ext}";
            var url = await _storage.UploadAsync(key, request.Content, contentType, cancellationToken);

            merchantBearing.AddImage(url);

            // v2.22.0 改图重审：图片是成交信任要素，与改价格/库存同口径，通过前商城不可见
            if (merchantBearing.ReviewStatus == ListingReviewStatus.Approved)
                merchantBearing.SubmitForApproval();

            await _merchantBearingRepository.UpdateAsync(merchantBearing, cancellationToken);

            _logger.LogInformation("商家商品图已追加: MerchantBearingId={Id}, Url={Url}, Count={Count}",
                merchantBearing.Id, url, merchantBearing.ImageUrls.Length);

            return merchantBearing.ImageUrls;
        }
    }
}
