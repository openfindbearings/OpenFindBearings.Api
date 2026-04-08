using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.SystemConfig.GetSystemConfigs
{
    /// <summary>
    /// 获取系统配置列表查询
    /// </summary>
    public record GetSystemConfigsQuery : IRequest<List<SystemConfigDto>>, IQuery
    {
        /// <summary>
        /// 配置分组（可选）
        /// </summary>
        public string? Group { get; init; }
    }
}
