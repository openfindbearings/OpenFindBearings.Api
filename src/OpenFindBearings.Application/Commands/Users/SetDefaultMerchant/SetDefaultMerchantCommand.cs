using MediatR;

namespace OpenFindBearings.Application.Commands.Users.SetDefaultMerchant
{
    /// <summary>
    /// 设置默认商户（v1.59.17）：决定商户 Tab 直达与冷启动优先进入的商户。
    /// 只能设置为自己在职成员关系内的商户，否则拒绝。
    /// </summary>
    public record SetDefaultMerchantCommand(Guid UserId, Guid MerchantId) : IRequest;
}