using MediatR;

namespace OpenFindBearings.Application.Features.History.Commands
{
    /// <summary>
    /// 清空浏览历史命令
    /// </summary>
    public record ClearHistoryCommand(string UserId) : IRequest;
}
