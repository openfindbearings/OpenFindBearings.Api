namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 寻货应答状态（v2.15.0 Tier2 类型安全化）：替代原 SourcingResponse.Status 裸 int 常量。
    /// 值与原常量保持一致（1~3），DB 列不变。
    /// 改动说明（v1.8.2 撤销应答软取消）：新增 Cancelled=4——撤销应答由"物理删行"改为"改状态留痕"，
    ///   使当日应答额度与"每日三件套"的应答项不再随撤销回退（额度一次消耗即为消耗），行本身保留可审计
    /// </summary>
    public enum SourcingResponseStatus
    {
        /// <summary>待处理（未被选定也未落选）</summary>
        Pending = 1,

        /// <summary>被采纳（发布人选定）</summary>
        Adopted = 2,

        /// <summary>未选中（发布人选定了别家，落选）</summary>
        NotSelected = 3,

        /// <summary>已撤销（商户在开标前撤回本商户应答；不参与比价与"是否已应答"判定，但计入当日额度）</summary>
        Cancelled = 4
    }
}