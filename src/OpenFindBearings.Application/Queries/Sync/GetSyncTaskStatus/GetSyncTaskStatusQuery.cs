using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Sync.GetSyncTaskStatus
{
    /// <summary>
    /// 获取同步任务状态查询
    /// </summary>
    public record GetSyncTaskStatusQuery : IRequest<SyncResultDto?>, IQuery
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        public Guid TaskId { get; init; }
    }
}
