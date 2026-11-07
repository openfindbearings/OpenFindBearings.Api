using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Services;

namespace OpenFindBearings.Application.Commands.MerchantBearings.Batch
{
    /// <summary>
    /// 商家商品批量操作命令处理器
    /// 职责：一次加载选中商品，逐件按状态判定可执行性（不可执行的转成跳过原因），
    ///       只做一次事务提交，避免单件失败拖垮整批；调价复用 PriceTextAdjuster 识别金额。
    /// </summary>
    public class BatchMerchantBearingCommandHandler : IRequestHandler<BatchMerchantBearingCommand, BatchMerchantBearingResult>
    {
        private readonly IMerchantBearingRepository _merchantBearingRepository;
        private readonly IMerchantMemberRepository _merchantMemberRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IPriceConfigProvider _priceConfigProvider;
        private readonly IMerchantGradeService _merchantGrades;
        private readonly IPointsService _pointsService;
        private readonly ILogger<BatchMerchantBearingCommandHandler> _logger;

        public BatchMerchantBearingCommandHandler(
            IMerchantBearingRepository merchantBearingRepository,
            IMerchantMemberRepository merchantMemberRepository,
            IMerchantRepository merchantRepository,
            IPriceConfigProvider priceConfigProvider,
            IMerchantGradeService merchantGrades,
            IPointsService pointsService,
            ILogger<BatchMerchantBearingCommandHandler> logger)
        {
            _merchantBearingRepository = merchantBearingRepository;
            _merchantMemberRepository = merchantMemberRepository;
            _merchantRepository = merchantRepository;
            _priceConfigProvider = priceConfigProvider;
            _merchantGrades = merchantGrades;
            _pointsService = pointsService;
            _logger = logger;
        }

        /// <summary>
        /// 执行批量操作：校验归属 → 逐件判定 → 统一标记修改/删除
        /// </summary>
        public async Task<BatchMerchantBearingResult> Handle(
            BatchMerchantBearingCommand request,
            CancellationToken cancellationToken)
        {
            var ids = (request.Ids ?? [])
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
            {
                throw new InvalidOperationException("请选择要操作的商品");
            }

            if (ids.Count > BatchMerchantBearingCommand.MaxBatchSize)
            {
                throw new InvalidOperationException($"一次最多操作 {BatchMerchantBearingCommand.MaxBatchSize} 件商品");
            }

            var percent = request.Percent ?? 0m;

            if (request.Action is BatchMerchantBearingAction.AdjustPrice or BatchMerchantBearingAction.PreviewPrice)
            {
                if (percent == 0m)
                {
                    throw new InvalidOperationException("调价幅度不能为 0");
                }

                if (percent < BatchMerchantBearingCommand.MinPercent || percent > BatchMerchantBearingCommand.MaxPercent)
                {
                    throw new InvalidOperationException(
                        $"调价幅度需在 {BatchMerchantBearingCommand.MinPercent}% 到 {BatchMerchantBearingCommand.MaxPercent}% 之间");
                }
            }

            // 归属校验：当前用户必须为该商户在职成员，且请求只作用于当前商户的商品
            var member = await _merchantMemberRepository.GetActiveByUserAndMerchantAsync(
                request.UserId, request.MerchantId, cancellationToken);
            if (member == null)
            {
                throw new UnauthorizedAccessException("无权操作该商户的商品");
            }

            var items = await _merchantBearingRepository.GetByIdsAsync(request.MerchantId, ids, cancellationToken);
            var itemMap = items.ToDictionary(mb => mb.Id);

            // 调价配置只在需要时读取一次
            var priceConfig = request.Action is BatchMerchantBearingAction.AdjustPrice or BatchMerchantBearingAction.PreviewPrice
                ? await _priceConfigProvider.GetAsync(cancellationToken)
                : null;

            var results = new List<BatchMerchantBearingItemResult>(ids.Count);
            var mutated = new List<MerchantBearing>();
            var deleting = new List<MerchantBearing>();

            foreach (var id in ids)
            {
                if (!itemMap.TryGetValue(id, out var merchantBearing))
                {
                    results.Add(new BatchMerchantBearingItemResult(id, null, false, "商品不存在或不属于当前商户"));
                    continue;
                }

                var partNumber = merchantBearing.Bearing?.PartNumber;

                switch (request.Action)
                {
                    case BatchMerchantBearingAction.PutOnShelf:
                        results.Add(ApplyPutOnShelf(merchantBearing, partNumber, mutated));
                        break;

                    case BatchMerchantBearingAction.TakeOffShelf:
                        results.Add(ApplyTakeOffShelf(merchantBearing, partNumber, mutated));
                        break;

                    case BatchMerchantBearingAction.Restock:
                        results.Add(ApplyRestock(merchantBearing, partNumber, request.RestockEta, mutated));
                        break;

                    case BatchMerchantBearingAction.AdjustPrice:
                    case BatchMerchantBearingAction.PreviewPrice:
                        results.Add(ApplyPriceChange(
                            merchantBearing,
                            partNumber,
                            percent,
                            priceConfig,
                            isPreview: request.Action == BatchMerchantBearingAction.PreviewPrice,
                            mutated));
                        break;

                    case BatchMerchantBearingAction.Delete:
                        results.Add(ApplyDelete(merchantBearing, partNumber, deleting));
                        break;

                    default:
                        throw new InvalidOperationException("不支持的批量操作类型");
                }
            }

            // 统一落库：修改走标记更新，删除走批量移除，事务由工作单元提交
            foreach (var merchantBearing in mutated)
            {
                await _merchantBearingRepository.UpdateAsync(merchantBearing, cancellationToken);
            }

            await _merchantBearingRepository.DeleteRangeAsync(deleting, cancellationToken);

            var succeeded = results.Count(r => r.Success);

            // 上架/下架/补货/删除都会改变在售件数，等级只重算一次
            if (mutated.Count > 0 || deleting.Count > 0)
            {
                await _merchantGrades.RecomputeAsync(request.MerchantId, cancellationToken);
            }

            // 首件商品上架一次性积分：与单件上架同口径，bizId 绑信用代码，重复批量也不会多发
            if (request.Action == BatchMerchantBearingAction.PutOnShelf && succeeded > 0)
            {
                await GrantFirstProductPointsAsync(request, cancellationToken);
            }

            _logger.LogInformation(
                "商品批量操作完成: Action={Action}, MerchantId={MerchantId}, 成功={Succeeded}, 跳过={Skipped}",
                request.Action, request.MerchantId, succeeded, results.Count - succeeded);

            return new BatchMerchantBearingResult(succeeded, results.Count - succeeded, results);
        }

        /// <summary>
        /// 批量上架：已通过的直接上架，仓库中/已驳回的提交审核，已在售或审核中的跳过
        /// </summary>
        private static BatchMerchantBearingItemResult ApplyPutOnShelf(
            MerchantBearing merchantBearing,
            string? partNumber,
            List<MerchantBearing> mutated)
        {
            if (merchantBearing.ReviewStatus == ListingReviewStatus.Approved)
            {
                if (merchantBearing.SaleState == SaleState.OnSale)
                {
                    return new BatchMerchantBearingItemResult(merchantBearing.Id, partNumber, false, "已在售");
                }

                merchantBearing.PutOnShelf();
                mutated.Add(merchantBearing);
                return new BatchMerchantBearingItemResult(merchantBearing.Id, partNumber, true, "已上架");
            }

            if (merchantBearing.ReviewStatus == ListingReviewStatus.Pending)
            {
                return new BatchMerchantBearingItemResult(merchantBearing.Id, partNumber, false, "审核中，请等待平台处理");
            }

            merchantBearing.SubmitForApproval();
            mutated.Add(merchantBearing);
            return new BatchMerchantBearingItemResult(merchantBearing.Id, partNumber, true, "已提交审核");
        }

        /// <summary>
        /// 批量下架：出售中/补货中的执行下架，已下架的跳过
        /// </summary>
        private static BatchMerchantBearingItemResult ApplyTakeOffShelf(
            MerchantBearing merchantBearing,
            string? partNumber,
            List<MerchantBearing> mutated)
        {
            if (merchantBearing.SaleState == SaleState.OffShelf)
            {
                return new BatchMerchantBearingItemResult(merchantBearing.Id, partNumber, false, "已下架");
            }

            merchantBearing.TakeOffShelf();
            mutated.Add(merchantBearing);
            return new BatchMerchantBearingItemResult(merchantBearing.Id, partNumber, true, "已下架");
        }

        /// <summary>
        /// 批量置为补货中：仅已通过审核的商品可置，其余跳过
        /// </summary>
        private static BatchMerchantBearingItemResult ApplyRestock(
            MerchantBearing merchantBearing,
            string? partNumber,
            string? restockEta,
            List<MerchantBearing> mutated)
        {
            if (merchantBearing.ReviewStatus != ListingReviewStatus.Approved)
            {
                return new BatchMerchantBearingItemResult(merchantBearing.Id, partNumber, false, "审核未通过，无法置为补货中");
            }

            merchantBearing.MarkAsRestocking(restockEta);
            mutated.Add(merchantBearing);
            return new BatchMerchantBearingItemResult(merchantBearing.Id, partNumber, true, "已置为补货中");
        }

        /// <summary>
        /// 批量调价：按百分比改写价格文本中的金额；预演模式只返回前后文本不落库
        /// </summary>
        private static BatchMerchantBearingItemResult ApplyPriceChange(
            MerchantBearing merchantBearing,
            string? partNumber,
            decimal percent,
            PriceConfigDto? priceConfig,
            bool isPreview,
            List<MerchantBearing> mutated)
        {
            var oldDescription = merchantBearing.PriceDescription;

            if (!PriceTextAdjuster.TryAdjustByPercent(oldDescription, percent, out var newDescription, out var reason))
            {
                return new BatchMerchantBearingItemResult(
                    merchantBearing.Id, partNumber, false, reason ?? "价格无法按比例调整");
            }

            // 价格描述列长度上限 100，加小数位后可能超长，交给商家单独编辑
            if (newDescription.Length > 100)
            {
                return new BatchMerchantBearingItemResult(
                    merchantBearing.Id, partNumber, false, "调整后的价格文本过长，请单独编辑");
            }

            if (isPreview)
            {
                return new BatchMerchantBearingItemResult(
                    merchantBearing.Id, partNumber, true, "可调整", oldDescription, newDescription);
            }

            var numericPrice = PriceParser.ExtractNumericPrice(newDescription, priceConfig?.ExtractPattern);
            merchantBearing.SetPrice(newDescription, numericPrice, merchantBearing.PriceVisibility);

            // 与单件编辑同口径：已通过审核的商品改价需重新送审，通过前公域不可见
            if (merchantBearing.ReviewStatus == ListingReviewStatus.Approved)
            {
                merchantBearing.SubmitForApproval();
            }

            mutated.Add(merchantBearing);
            return new BatchMerchantBearingItemResult(
                merchantBearing.Id, partNumber, true, "已调整价格", oldDescription, newDescription);
        }

        /// <summary>
        /// 批量删除：出售中/补货中的商品按守卫跳过，其余标记物理删除
        /// </summary>
        private static BatchMerchantBearingItemResult ApplyDelete(
            MerchantBearing merchantBearing,
            string? partNumber,
            List<MerchantBearing> deleting)
        {
            try
            {
                merchantBearing.EnsureRemovable();
            }
            catch (InvalidOperationException ex)
            {
                return new BatchMerchantBearingItemResult(merchantBearing.Id, partNumber, false, ex.Message);
            }

            deleting.Add(merchantBearing);
            return new BatchMerchantBearingItemResult(merchantBearing.Id, partNumber, true, "已删除");
        }

        /// <summary>
        /// 发放首件商品上架一次性积分：绑定信用代码做幂等，任何一件成功即可触发，
        /// 无信用代码的商户跳过（与单件上架处理器保持同一口径）
        /// </summary>
        private async Task GrantFirstProductPointsAsync(
            BatchMerchantBearingCommand request,
            CancellationToken cancellationToken)
        {
            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken);
            if (string.IsNullOrWhiteSpace(merchant?.UnifiedSocialCreditCode))
            {
                return;
            }

            await _pointsService.GrantOneTimeAsync(
                request.UserId,
                PointTransaction.TypeMerchantFirstProduct,
                $"credit:{merchant.UnifiedSocialCreditCode.Trim().ToUpperInvariant()}:product",
                $"商户「{merchant.Name}」首件商品上架",
                cancellationToken: cancellationToken);
        }
    }
}
