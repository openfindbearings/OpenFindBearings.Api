using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Merchants.RequestVerifyMerchant
{
    /// <summary>
    /// 商户主动申请认证命令（v2.9.0 申请-审核闭环）：
    /// 资格按材料矩阵即时校验（与 Admin 认证同口径），通过后置 VerifyRequested 供审核队列优先处理
    /// </summary>
    public record RequestVerifyMerchantCommand(Guid MerchantId, Guid OperatorUserId) : IRequest, ICommand;
}
