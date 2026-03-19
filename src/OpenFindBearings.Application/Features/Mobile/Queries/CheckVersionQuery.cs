using MediatR;
using OpenFindBearings.Application.Features.Mobile.DTOs;

namespace OpenFindBearings.Application.Features.Mobile.Queries
{
    /// <summary>
    /// 版本检查查询
    /// </summary>
    public record CheckVersionQuery : IRequest<VersionCheckResult>
    {
        public string CurrentVersion { get; init; } = string.Empty;
        public string Platform { get; init; } = string.Empty;
    }
}
