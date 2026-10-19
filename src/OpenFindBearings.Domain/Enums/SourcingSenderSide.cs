namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 撮合会话消息发送方（v2.18.0 T1 会话）：标识一条消息出自需求方还是商家一方，
    /// 用于会话气泡左右分栏、未读归属与"只读不回"的终态展示
    /// </summary>
    public enum SourcingSenderSide
    {
        /// <summary>需求方（发布人方）</summary>
        Publisher = 0,
        /// <summary>商家方</summary>
        Merchant = 1
    }
}
