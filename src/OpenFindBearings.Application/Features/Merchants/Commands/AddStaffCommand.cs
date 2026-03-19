using MediatR;

namespace OpenFindBearings.Application.Features.Merchants.Commands
{
    /// <summary>
    /// 添加员工命令
    /// </summary>
    public record AddStaffCommand : IRequest<Guid>
    {
        /// <summary>
        /// 商家ID
        /// </summary>
        public Guid MerchantId { get; init; }

        /// <summary>
        /// 员工邮箱
        /// </summary>
        public string Email { get; init; } = string.Empty;

        /// <summary>
        /// 员工角色
        /// </summary>
        public string? Role { get; init; }

        /// <summary>
        /// 操作人ID
        /// </summary>
        public Guid OperatorId { get; init; }
    }
}
