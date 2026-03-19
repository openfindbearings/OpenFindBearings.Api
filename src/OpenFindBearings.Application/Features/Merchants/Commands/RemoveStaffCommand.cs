using MediatR;

namespace OpenFindBearings.Application.Features.Merchants.Commands
{
    /// <summary>
    /// 移除员工命令
    /// </summary>
    public record RemoveStaffCommand : IRequest
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
