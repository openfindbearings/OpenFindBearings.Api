using MediatR;

namespace OpenFindBearings.Application.Features.Follows.Commands
{
    /// <summary>
    /// 关注商家命令
    /// </summary>
    public record FollowMerchantCommand(
        Guid MerchantId,
        string UserId  // AuthUserId
    ) : IRequest<bool>;
}
