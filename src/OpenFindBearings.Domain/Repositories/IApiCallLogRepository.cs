using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Repositories
{
    public interface IApiCallLogRepository
    {
        Task AddAsync(ApiCallLog log, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取指定用户在指定时间内的调用次数
        /// </summary>
        Task<int> GetCountByUserIdAsync(Guid userId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取指定IP在指定时间内的调用次数
        /// </summary>
        Task<int> GetCountByIpAsync(string ip, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取指定会话在指定时间内的调用次数
        /// </summary>
        Task<int> GetCountBySessionIdAsync(string sessionId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);
    }
}
