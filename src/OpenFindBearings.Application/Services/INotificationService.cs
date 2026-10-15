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

        /// <summary>
        /// 多渠道外发（v1.6.0 多方式提醒）：按用户偏好把同一内容扇出到启用的外部通道
        ///   （短信 / 微信 / 邮件）；未配置网关的通道只记日志不外发（预留）。与 AddInAppAsync 配对使用：
        ///   站内信必发，本方法负责外部提醒，达成"一次事件、多方式同时发出"。
        /// </summary>
        /// <param name="userId">收件人用户 ID</param>
        /// <param name="title">提醒标题</param>
        /// <param name="body">提醒正文</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task AddExternalAsync(Guid userId, string title, string body, CancellationToken cancellationToken = default);
    }
}
