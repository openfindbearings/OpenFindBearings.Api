namespace OpenFindBearings.Application.Interfaces
{
    /// <summary>
    /// 轴承查看统计服务
    /// </summary>
    public interface IBearingViewStatsService
    {
        Task RecordViewAsync(Guid bearingId, Guid? userId, string? sessionId, DateTime viewedAt, CancellationToken cancellationToken = default);
        Task<long> GetViewCountAsync(Guid bearingId, CancellationToken cancellationToken = default);
        Task<List<Guid>> GetHotBearingsAsync(int count, CancellationToken cancellationToken = default);
    }
}
