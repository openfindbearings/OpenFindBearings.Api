using MediatR;

namespace OpenFindBearings.Application.Features.History.Commands
{
    /// <summary>
    /// 记录轴承浏览历史命令
    /// </summary>
    public record RecordBearingViewCommand(
        Guid BearingId,
        string UserId  // AuthUserId
    ) : IRequest;
}
