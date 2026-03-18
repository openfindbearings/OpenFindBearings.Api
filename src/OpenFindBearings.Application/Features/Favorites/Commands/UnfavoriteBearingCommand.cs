using MediatR;

namespace OpenFindBearings.Application.Features.Favorites.Commands
{
    /// <summary>
    /// 取消收藏轴承命令
    /// </summary>
    public record UnfavoriteBearingCommand(
        Guid BearingId,
        string UserId  // AuthUserId
    ) : IRequest;
}
