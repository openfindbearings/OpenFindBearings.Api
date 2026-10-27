using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.MerchantBearings.ManageImages
{
    /// <summary>
    /// 删除商家商品实拍图处理器（v2.22.0）：仅移除图片列表中的地址，
    /// 不删除对象存储文件（历史审核记录可能引用）；已通过审核的商品自动重新送审。
    /// </summary>
    public class RemoveMerchantBearingImageCommandHandler : IRequestHandler<RemoveMerchantBearingImageCommand, string[]>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly ILogger<RemoveMerchantBearingImageCommandHandler> _logger;

        /// <summary>
        /// 构造函数：注入商家商品仓储、成员仓储与日志
        /// </summary>
        public RemoveMerchantBearingImageCommandHandler(
            IMerchantBearingRepository merchantBearingRepository,
            IMerchantMemberRepository merchantMemberRepository,
            ILogger<RemoveMerchantBearingImageCommandHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _merchantMemberRepository = merchantMemberRepository;
            _logger = logger;
        }

        /// <summary>
        /// 执行删除：成员校验 -> 移除地址 -> 必要时重新送审
        /// </summary>
        public async Task<string[]> Handle(RemoveMerchantBearingImageCommand request, CancellationToken cancellationToken)
        {
            var merchantBearing = await _merchantBearingRepository.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"商家-轴承关联不存在: {request.Id}");

            var member = await _merchantMemberRepository.GetActiveByUserAndMerchantAsync(
                request.UserId, merchantBearing.MerchantId, cancellationToken);
            if (member == null)
                throw new UnauthorizedAccessException("无权限修改该商家的商品信息");

            merchantBearing.RemoveImage(request.Url);

            if (merchantBearing.ReviewStatus == ListingReviewStatus.Approved)
                merchantBearing.SubmitForApproval();

            await _merchantBearingRepository.UpdateAsync(merchantBearing, cancellationToken);

            _logger.LogInformation("商家商品图已删除: MerchantBearingId={Id}, Url={Url}, Count={Count}",
                merchantBearing.Id, request.Url, merchantBearing.ImageUrls.Length);

            return merchantBearing.ImageUrls;
        }
    }
}
