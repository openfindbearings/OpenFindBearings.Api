using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Merchants.RemoveStaff
{
    /// <summary>
    /// 移除员工命令
    /// </summary>
    public record RemoveStaffCommand : IRequest, ICommand
    {
        /// <summary>
        /// 员工用户ID
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// 操作人ID
        /// </summary>
        public Guid OperatorId { get; init; }
    }
}
