using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Mobile.CheckVersion
{
    /// <summary>
    /// 版本检查查询
    /// </summary>
    public record CheckVersionQuery : IRequest<VersionCheckResult>, IQuery
    {
        public string CurrentVersion { get; init; } = string.Empty;
        public string Platform { get; init; } = string.Empty;
    }
}
