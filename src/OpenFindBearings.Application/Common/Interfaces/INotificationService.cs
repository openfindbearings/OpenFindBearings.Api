namespace OpenFindBearings.Application.Common.Interfaces
{
    /// <summary>
    /// 通知服务接口
    /// </summary>
    public interface INotificationService
    {
        Task SendToAdminsAsync(string title, string message, CancellationToken cancellationToken = default);
        Task SendToUserAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default);
    }
}
