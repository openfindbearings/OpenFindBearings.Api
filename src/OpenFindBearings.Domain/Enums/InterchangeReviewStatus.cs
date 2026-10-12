namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 替代关系审核状态（v2.16.0 替代品治理）。
    /// 分工模型：平台建标准、商家报候选、平台审核生效。
    /// 平台自建直接生效（Approved）；商家提交候选为待审核（Pending）；平台驳回为 Rejected（提交方可见、可改重提）。
    /// </summary>
    public enum InterchangeReviewStatus
    {
        /// <summary>已生效（平台自建 / 商家提交经平台审核通过）</summary>
        Approved = 0,

        /// <summary>待审核（商家提交，尚未公域可见）</summary>
        Pending = 1,

        /// <summary>已驳回（未通过，仅提交方可见）</summary>
        Rejected = 2
    }
}