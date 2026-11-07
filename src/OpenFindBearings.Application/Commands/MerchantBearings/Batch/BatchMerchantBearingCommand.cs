using MediatR;

namespace OpenFindBearings.Application.Commands.MerchantBearings.Batch
{
    /// <summary>
    /// 商家商品批量操作类型
    /// </summary>
    public enum BatchMerchantBearingAction
    {
        /// <summary>批量上架（已通过的直接上架，仓库中/已驳回的提交审核）</summary>
        PutOnShelf = 0,

        /// <summary>批量下架</summary>
        TakeOffShelf = 1,

        /// <summary>批量置为补货中</summary>
        Restock = 2,

        /// <summary>按百分比批量调整价格文本</summary>
        AdjustPrice = 3,

        /// <summary>预演按百分比调价，不写库，仅返回调整前后文本</summary>
        PreviewPrice = 4,

        /// <summary>批量删除（出售中/补货中的会被跳过）</summary>
        Delete = 5
    }

    /// <summary>
    /// 商家商品批量操作命令
    /// 职责：一次请求对多件商品执行同一动作，逐件返回成功或跳过原因，避免整批失败
    /// </summary>
    public record BatchMerchantBearingCommand : IRequest<BatchMerchantBearingResult>
    {
        /// <summary>
        /// 当前商户ID（API 端点按 X-Merchant-Id 上下文注入）
        /// </summary>
        public Guid MerchantId { get; init; }

        /// <summary>
        /// 当前用户ID（用于商户成员归属校验）
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// 批量动作
        /// </summary>
        public BatchMerchantBearingAction Action { get; init; }

        /// <summary>
        /// 要操作的商品关联ID集合（去重后最多 <see cref="MaxBatchSize"/> 件）
        /// </summary>
        public IReadOnlyList<Guid> Ids { get; init; } = [];

        /// <summary>
        /// 调价幅度（正数涨价、负数降价，仅调价动作使用）
        /// </summary>
        public decimal? Percent { get; init; }

        /// <summary>
        /// 补货预计到货时间（自由文本，仅补货动作使用）
        /// </summary>
        public string? RestockEta { get; init; }

        /// <summary>
        /// 单次批量操作上限，防止误选整店或超时
        /// </summary>
        public const int MaxBatchSize = 100;

        /// <summary>
        /// 调价幅度允许区间（百分比），超出视为手滑
        /// </summary>
        public const decimal MinPercent = -80m;

        /// <summary>
        /// 调价幅度允许区间（百分比）
        /// </summary>
        public const decimal MaxPercent = 200m;
    }
}
