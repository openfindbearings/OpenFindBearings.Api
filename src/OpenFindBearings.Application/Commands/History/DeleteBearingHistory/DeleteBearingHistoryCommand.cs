using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.History.DeleteBearingHistory
{
    /// <summary>
    /// 删除单条轴承浏览历史命令（按 userId+bearingId 定位，防越权删他人记录）
    /// </summary>
    public record DeleteBearingHistoryCommand : IRequest, ICommand
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// 轴承ID（历史行按用户+轴承唯一定位）
        /// </summary>
        public Guid BearingId { get; init; }
    }
}
