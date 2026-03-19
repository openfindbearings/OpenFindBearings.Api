using MediatR;

namespace OpenFindBearings.Application.Features.History.Commands
{
    /// <summary>
    /// 记录轴承浏览历史命令
    /// </summary>
    public record RecordBearingViewCommand : IRequest
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// 轴承ID
        /// </summary>
        public Guid BearingId { get; init; }
    }
}
