using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Commands.Merchants.AddStaff
{
    /// <summary>
    /// 添加员工命令
    /// </summary>
    public record AddStaffCommand : IRequest<AddStaffResult>, ICommand
    {
        /// <summary>
        /// 商家ID
        /// </summary>
        public Guid MerchantId { get; init; }

        /// <summary>
        /// 员工邮箱（与手机号二选一）
        /// </summary>
        public string? Email { get; init; }

        /// <summary>
        /// 员工手机号（与邮箱二选一）
        /// </summary>
        public string? Phone { get; init; }

        /// <summary>
        /// 员工角色
        /// </summary>
        public string? Role { get; init; }

        /// <summary>
        /// 操作人ID
        /// </summary>
        public Guid OperatorId { get; init; }

        /// <summary>
        /// 获取主要联系方式
        /// </summary>
        public string GetContactInfo() => Email ?? Phone ?? string.Empty;
    }
}
