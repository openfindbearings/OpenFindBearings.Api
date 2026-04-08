using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.History.ClearHistory
{
    /// <summary>
    /// 清空浏览历史命令
    /// </summary>
    public record ClearHistoryCommand : IRequest, ICommand
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; init; }
    }
}
