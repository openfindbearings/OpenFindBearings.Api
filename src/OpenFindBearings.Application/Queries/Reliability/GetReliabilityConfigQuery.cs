using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Reliability
{
    /// <summary>
    /// 获取可信度阈值配置查询
    /// 职责：从 SystemConfigs 读取同步流程使用的可信度三阈值，供 Sync 运行时拉取
    /// </summary>
    public class GetReliabilityConfigQuery : IRequest<ReliabilityConfigDto>, IQuery
    {
    }

    /// <summary>
    /// 查询处理器
    /// </summary>
    public class GetReliabilityConfigQueryHandler : IRequestHandler<GetReliabilityConfigQuery, ReliabilityConfigDto>
    {
        private readonly ISystemConfigRepository _systemConfigRepository;

        public GetReliabilityConfigQueryHandler(ISystemConfigRepository systemConfigRepository)
        {
            _systemConfigRepository = systemConfigRepository;
        }

        public async Task<ReliabilityConfigDto> Handle(
            GetReliabilityConfigQuery request,
            CancellationToken cancellationToken)
        {
            // 改动说明：从 SystemConfigs 读取三阈值，缺省回退到代码内置默认值，保证配置缺失时不崩溃
            var autoSyncThreshold = await _systemConfigRepository.GetValueAsync<int>(
                "Reliability.AutoSyncThreshold", 85, cancellationToken);
            var reviewThreshold = await _systemConfigRepository.GetValueAsync<int>(
                "Reliability.ReviewThreshold", 60, cancellationToken);
            var defaultSourceScore = await _systemConfigRepository.GetValueAsync<int>(
                "Reliability.DefaultSourceScore", 80, cancellationToken);

            return new ReliabilityConfigDto
            {
                AutoSyncThreshold = autoSyncThreshold,
                ReviewThreshold = reviewThreshold,
                DefaultSourceScore = defaultSourceScore
            };
        }
    }
}
