using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Application.Commands.Merchants.ChangeMerchantMemberRole
{
    /// <summary>
    /// 变更商户成员角色命令
    /// </summary>
    public record ChangeMerchantMemberRoleCommand : IRequest, ICommand
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
        /// 新角色（MerchantAdmin / MerchantStaff）
        /// </summary>
        public string Role { get; init; } = MerchantMember.RoleMerchantStaff;

        /// <summary>
        /// 操作人ID
        /// </summary>
        public Guid OperatorId { get; init; }
    }
}
