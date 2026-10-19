namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 寻货曝光范围（v1.8.0 公开招标/邀标）：替代原"定向询价"单字段 TargetMerchantId——
    /// 那时一个字段既限定受众为一家、又决定不进大厅，把"受众"与"曝光"两件事混在一起。
    /// 现拆为"曝光范围（本枚举）+ 邀标收件人快照（InvitedMerchantIds）"，
    /// 采购语义对齐招投标：有在售商家的走邀标，没有在售的走公开招标。
    /// 值 0 = 公开为默认，存量数据与 EF 列默认值均落在公开侧。
    /// </summary>
    public enum SourcingVisibility
    {
        /// <summary>
        /// 公开招标（默认）：需求进入公开大厅（人人可见可应答），同时把需求推送给
        /// 匹配该型号的认证在售商家。
        /// </summary>
        Public = 0,

        /// <summary>
        /// 邀标：需求不进公开大厅，只推送给发布时点勾选的认证在售商家（InvitedMerchantIds 快照），
        /// 仅这些商家与发布人可见可应答。
        /// </summary>
        Invited = 1
    }
}
