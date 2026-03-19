using MediatR;
using OpenFindBearings.Application.Features.Sync.DTOs;

namespace OpenFindBearings.Application.Features.Sync.Queries
{
    /// <summary>
    /// 获取同步任务状态查询
    /// </summary>
    public record GetSyncTaskStatusQuery : IRequest<SyncResultDto?>
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        public Guid TaskId { get; init; }
    }
}
