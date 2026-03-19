using MediatR;
using OpenFindBearings.Application.Features.Admin.DTOs;

namespace OpenFindBearings.Application.Features.SystemConfig.Queries
{
    /// <summary>
    /// 获取系统配置列表查询
    /// </summary>
    public record GetSystemConfigsQuery : IRequest<List<SystemConfigDto>>
    {
        /// <summary>
        /// 配置分组（可选）
        /// </summary>
        public string? Group { get; init; }
    }
}
