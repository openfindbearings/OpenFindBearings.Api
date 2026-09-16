namespace OpenFindBearings.Application.Services
{
    /// <summary>
    /// 通知服务接口
    /// </summary>
    public interface INotificationService
    {
        Task SendToAdminsAsync(string title, string message, CancellationToken cancellationToken = default);
        Task SendToUserAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default);

        /// <summary>
        /// 写一条站内信（落 Notifications 表并独立提交）。
        /// 改动说明：通知通道抽象——当前仅站内信；后续短信/订阅消息在此接口扩展兄弟方法，
        ///   事件订阅者不感知具体通道。
        /// </summary>
        /// <param name="userId">收件人用户 ID</param>
        /// <param name="type">Notification.Type* 常量</param>
        /// <param name="title">标题</param>
        /// <param name="body">正文</param>
        /// <param name="bizType">业务类型（Notification.Biz* 常量，决定前端跳转）</param>
        /// <param name="bizId">业务对象 ID</param>
        Task AddInAppAsync(Guid userId, string type, string title, string body,
            string? bizType = null, Guid? bizId = null, CancellationToken cancellationToken = default);
    }
}
