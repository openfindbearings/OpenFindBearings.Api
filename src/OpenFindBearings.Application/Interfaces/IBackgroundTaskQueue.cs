namespace OpenFindBearings.Application.Interfaces
{
    /// <summary>
    /// 后台任务队列接口
    /// </summary>
    public interface IBackgroundTaskQueue
    {
        /// <summary>
        /// 将任务加入队列
        /// </summary>
        void QueueBackgroundWorkItem(Func<IServiceProvider, CancellationToken, Task> workItem);

        /// <summary>
        /// 从队列中取出任务
        /// </summary>
        Task<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
    }
}
