namespace OpenFindBearings.Application.Commands.MerchantBearings.Batch
{
    /// <summary>
    /// 批量操作单件结果
    /// </summary>
    /// <param name="Id">商品关联ID</param>
    /// <param name="PartNumber">轴承型号（便于前端直接展示，找不到记录时为空）</param>
    /// <param name="Success">是否执行成功</param>
    /// <param name="Message">成功提示或跳过原因</param>
    /// <param name="OldPriceDescription">调价前的价格文本（仅调价动作返回）</param>
    /// <param name="NewPriceDescription">调价后的价格文本（仅调价动作返回）</param>
    public record BatchMerchantBearingItemResult(
        Guid Id,
        string? PartNumber,
        bool Success,
        string? Message,
        string? OldPriceDescription = null,
        string? NewPriceDescription = null);

    /// <summary>
    /// 批量操作汇总结果
    /// </summary>
    /// <param name="Succeeded">成功件数</param>
    /// <param name="Skipped">跳过件数</param>
    /// <param name="Results">逐件结果（按请求顺序返回）</param>
    public record BatchMerchantBearingResult(
        int Succeeded,
        int Skipped,
        IReadOnlyList<BatchMerchantBearingItemResult> Results);
}
