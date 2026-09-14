namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 商户入驻渠道标记。用于申请人"撤回申请"时区分清理策略：
    ///   Self 新建的待审商户撤回时硬删除（未公示、避免僵尸数据干扰同名再新建查重）；
    ///   Claim 认领的既有商户撤回时仅解除认领人成员并把来源退回爬虫（不删商户本体）；
    ///   Nomination 提名草稿撤回时作废草稿并撤销邀请（不在申请人自助撤回入口处理）。
    /// 默认 None 兼容历史/爬虫/种子数据。
    /// </summary>
    public enum ApplicationMode
    {
        /// <summary>
        /// 非入驻流程产生（爬虫、导入、种子、历史数据）
        /// </summary>
        None = 0,

        /// <summary>
        /// 自助新建
        /// </summary>
        Self = 1,

        /// <summary>
        /// 认领既有（爬虫）商家
        /// </summary>
        Claim = 2,

        /// <summary>
        /// 提名他人为管理员
        /// </summary>
        Nomination = 3
    }
}
