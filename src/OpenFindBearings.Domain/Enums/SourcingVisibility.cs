namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 寻货曝光范围（v1.8.0 公开/邀请）：替代原"定向询价"单字段 TargetMerchantId——
    /// 那时一个字段既限定受众为一家、又决定不进大厅，把"受众"与"曝光"两件事混在一起。
    /// 现拆为"曝光范围（本枚举）+ 邀请收件人快照（InvitedMerchantIds）"，
    /// 使用口径：有在售商家时可由发布人点名邀请，没有在售时只能公开。
    /// 值 0 = 公开为默认，存量数据与 EF 列默认值均落在公开侧。
    /// </summary>
    public enum SourcingVisibility
    {
        /// <summary>
        /// 公开（默认）：需求进入公开大厅（人人可见可应答），同时把需求推送给
        /// 匹配该型号的认证在售商家。
        /// </summary>
        Public = 0,

        /// <summary>
        /// 邀请：需求不进公开大厅，只推送给发布时点勾选的认证在售商家（InvitedMerchantIds 快照），
        /// 仅这些商家与发布人可见可应答。
        /// </summary>
        Invited = 1
    }
}
