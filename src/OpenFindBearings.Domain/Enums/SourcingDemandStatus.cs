namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 寻货需求状态（v2.15.0 Tier2 类型安全化）：替代原 SourcingDemand.Status 裸 int 常量。
    /// 值与原常量保持一致（1~5），DB 列不变。
    /// </summary>
    public enum SourcingDemandStatus
    {
        /// <summary>进行中（可被应答）</summary>
        Published = 1,

        /// <summary>已选定关闭（发布人确认了某条应答，双方解锁联系方式）</summary>
        Closed = 2,

        /// <summary>已过期（发布后 14 天无人确认，读时惰性判定）</summary>
        Expired = 3,

        /// <summary>发布人主动取消</summary>
        Cancelled = 4,

        /// <summary>平台下架（违规内容，Admin 治理）</summary>
        TakenDown = 5
    }
}