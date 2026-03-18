using MediatR;

namespace OpenFindBearings.Application.Features.Favorites.Commands
{
    /// <summary>
    /// 收藏轴承命令
    /// </summary>
    public record FavoriteBearingCommand(
        Guid BearingId,
        string UserId  // AuthUserId
    ) : IRequest<bool>;
}
