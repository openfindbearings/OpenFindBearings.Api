using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Features.Sync.DTOs;
using OpenFindBearings.Application.Features.Sync.Queries;

namespace OpenFindBearings.Application.Features.Sync.Handlers
{
    /// <summary>
    /// 获取同步任务状态查询处理器
    /// </summary>
    public class GetSyncTaskStatusQueryHandler : IRequestHandler<GetSyncTaskStatusQuery, SyncResultDto?>
    {
        private readonly ILogger<GetSyncTaskStatusQueryHandler> _logger;

        public GetSyncTaskStatusQueryHandler(ILogger<GetSyncTaskStatusQueryHandler> logger)
        {
            _logger = logger;
        }

        public async Task<SyncResultDto?> Handle(GetSyncTaskStatusQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("获取同步任务状态: TaskId={TaskId}", request.TaskId);

            // TODO: 从缓存或数据库获取任务状态
            // 这里需要实现任务状态存储机制

            // 临时返回示例数据
            return await Task.FromResult(new SyncResultDto
            {
                TaskId = request.TaskId,
                SuccessCount = 95,
                FailCount = 5,
                ElapsedSeconds = 12.5,
                Errors = new List<SyncErrorDto>
                {
                    new() { Identifier = "6205", Error = "品牌代码 SKF 不存在" },
                    new() { Identifier = "6206", Error = "轴承类型代码 DGBB 不存在" }
                }
            });
        }
    }
}
