using MediatR;

namespace OpenFindBearings.Application.Features.Follows.Commands
{
    /// <summary>
    /// 取消关注商家命令
    /// </summary>
    public record UnfollowMerchantCommand(
        Guid MerchantId,
        string UserId  // AuthUserId
    ) : IRequest;
}
