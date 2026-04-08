using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Favorites.FavoriteBearing
{
    /// <summary>
    /// 收藏轴承命令
    /// </summary>
    public record FavoriteBearingCommand : IRequest<bool>, ICommand
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
