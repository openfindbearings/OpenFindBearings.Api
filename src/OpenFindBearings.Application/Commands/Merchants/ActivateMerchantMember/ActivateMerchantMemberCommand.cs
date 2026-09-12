using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Merchants.ActivateMerchantMember
{
    /// <summary>
    /// 恢复被停用的商户成员命令
    /// </summary>
    public record ActivateMerchantMemberCommand : IRequest, ICommand
    {
        /// <summary>
        /// 目标成员用户ID
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// 商户ID（当前商户上下文）
        /// </summary>
        public Guid MerchantId { get; init; }

        /// <summary>
        /// 操作人ID
        /// </summary>
        public Guid OperatorId { get; init; }
    }
}
