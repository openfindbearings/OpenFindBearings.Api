using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Merchants.SuspendMerchantMember
{
    /// <summary>
    /// 停用商户成员命令（离职/异常处置，保留成员关系但立即失去操作权限，可恢复）
    /// </summary>
    public record SuspendMerchantMemberCommand : IRequest, ICommand
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
