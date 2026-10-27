using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.MerchantBearings.ManageImages
{
    /// <summary>
    /// 重排商家商品实拍图处理器（v2.22.0）：首项即首图，集合必须与现有图片完全一致；
    /// 已通过审核的商品调整首图后自动重新送审。
    /// </summary>
    public class ReorderMerchantBearingImagesCommandHandler : IRequestHandler<ReorderMerchantBearingImagesCommand, string[]>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly ILogger<ReorderMerchantBearingImagesCommandHandler> _logger;

        /// <summary>
        /// 构造函数：注入商家商品仓储、成员仓储与日志
        /// </summary>
        public ReorderMerchantBearingImagesCommandHandler(
            IMerchantBearingRepository merchantBearingRepository,
            IMerchantMemberRepository merchantMemberRepository,
            ILogger<ReorderMerchantBearingImagesCommandHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _merchantMemberRepository = merchantMemberRepository;
            _logger = logger;
        }

        /// <summary>
        /// 执行重排：成员校验 -> 校验集合并写序 -> 必要时重新送审
        /// </summary>
        public async Task<string[]> Handle(ReorderMerchantBearingImagesCommand request, CancellationToken cancellationToken)
        {
            var merchantBearing = await _merchantBearingRepository.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"商家-轴承关联不存在: {request.Id}");

            var member = await _merchantMemberRepository.GetActiveByUserAndMerchantAsync(
                request.UserId, merchantBearing.MerchantId, cancellationToken);
            if (member == null)
                throw new UnauthorizedAccessException("无权限修改该商家的商品信息");

            var before = merchantBearing.ImageUrls;
            merchantBearing.SetImageOrder(request.Urls);

            // 顺序没变就不必打扰审核（避免只点一下"设为首图"却没变化也进入待审）
            var changed = !before.SequenceEqual(merchantBearing.ImageUrls);
            if (changed && merchantBearing.ReviewStatus == ListingReviewStatus.Approved)
                merchantBearing.SubmitForApproval();

            await _merchantBearingRepository.UpdateAsync(merchantBearing, cancellationToken);

            _logger.LogInformation("商家商品图已重排: MerchantBearingId={Id}, Count={Count}, Changed={Changed}",
                merchantBearing.Id, merchantBearing.ImageUrls.Length, changed);

            return merchantBearing.ImageUrls;
        }
    }
}
