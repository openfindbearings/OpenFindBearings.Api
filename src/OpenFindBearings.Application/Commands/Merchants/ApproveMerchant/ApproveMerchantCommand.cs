using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Merchants.ApproveMerchant
{
    /// <summary>
    /// 审核通过入驻申请命令（Pending -> Active）
    /// 与认证（Verify，置 IsVerified）分离：approve 使商户生效可被C端检索，认证提升等级
    /// </summary>
    public record ApproveMerchantCommand(Guid Id, string? ApprovedBy = null) : IRequest, ICommand;
}
