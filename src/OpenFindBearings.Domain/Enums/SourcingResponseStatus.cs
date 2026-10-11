namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 寻货应答状态（v2.15.0 Tier2 类型安全化）：替代原 SourcingResponse.Status 裸 int 常量。
    /// 值与原常量保持一致（1~3），DB 列不变。
    /// </summary>
    public enum SourcingResponseStatus
    {
        /// <summary>待处理（未被选定也未落选）</summary>
        Pending = 1,

        /// <summary>被采纳（发布人选定）</summary>
        Adopted = 2,

        /// <summary>未选中（发布人选定了别家，落选）</summary>
        NotSelected = 3
    }
}