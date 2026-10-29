namespace OpenFindBearings.Domain.Enums
{
    /// <summary>
    /// 意见反馈处理状态
    /// 仅用于管理端分诊流转，用户侧只感知"已回复/未回复"
    /// </summary>
    public enum FeedbackStatus
    {
        /// <summary>待处理（新提交，默认）</summary>
        Pending = 0,

        /// <summary>处理中（管理员已认领）</summary>
        Processing = 1,

        /// <summary>已处理（已回复或已确认无需处理）</summary>
        Resolved = 2,

        /// <summary>已关闭（重复提交、无效反馈等）</summary>
        Closed = 3
    }
}
