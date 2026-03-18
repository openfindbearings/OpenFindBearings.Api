using MediatR;

namespace OpenFindBearings.Application.Features.Merchants.Commands
{
    /// <summary>
    /// 认证商家命令
    /// </summary>
    public record VerifyMerchantCommand(Guid Id) : IRequest;
}
