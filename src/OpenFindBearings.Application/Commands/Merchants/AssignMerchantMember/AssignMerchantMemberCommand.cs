using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Merchants.AssignMerchantMember
{
    /// <summary>
    /// 平台指定商户成员命令（G4 兜底通道）
    /// 商户无在职管理员等异常时，由平台直接指定/恢复成员，避免商户永久失去管理者
    /// </summary>
    public record AssignMerchantMemberCommand(Guid MerchantId, Guid UserId, string Role) : IRequest<Guid>, ICommand
    {
        /// <summary>
        /// 操作人（平台管理员用户ID，由端点注入）
        /// </summary>
        public Guid OperatorId { get; init; }
    }
}
