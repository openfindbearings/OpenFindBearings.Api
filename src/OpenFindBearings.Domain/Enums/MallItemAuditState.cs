namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 商城礼品审核状态（v2.15.0 Tier2 类型安全化）：替代原 MallItem.AuditState 裸 int 常量。
    /// 值与原常量保持一致（0~3），DB 列不变。仅商家挂礼（Gift）有审核语义，其余恒 NotApplicable。
    /// </summary>
    public enum MallItemAuditState
    {
        /// <summary>平台内置权益，无需审核</summary>
        NotApplicable = 0,

        /// <summary>待审核（商家挂礼提交后）</summary>
        Pending = 1,

        /// <summary>已通过（平台定档放行，可兑换）</summary>
        Approved = 2,

        /// <summary>已驳回（平台拒绝，商家可见理由）</summary>
        Rejected = 3
    }
}