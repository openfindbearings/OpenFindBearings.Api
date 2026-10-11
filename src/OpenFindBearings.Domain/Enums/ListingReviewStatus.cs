namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 商家商品审核状态（v2.15.0 状态统一）：替代原 IsPendingApproval bool。
    /// 审核位决定"是否属平台已放行"，与销售态（SaleState）正交；
    /// 公域可见 = ReviewStatus==Approved 且销售态为出售中/补货中。
    /// </summary>
    public enum ListingReviewStatus
    {
        /// <summary>仓库中（商家私库，从未提交审核，仅商家后台可见）</summary>
        None = 0,

        /// <summary>审核中（已提交，等待平台审核，仅商家后台可见）</summary>
        Pending = 1,

        /// <summary>已通过（平台放行，可公域展示）</summary>
        Approved = 2,

        /// <summary>已驳回（平台驳回，带理由，仅商家后台可见，可修改重提）</summary>
        Rejected = 3
    }
}