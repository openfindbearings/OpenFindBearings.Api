using MediatR;

namespace OpenFindBearings.Application.Features.Merchants.Commands
{
    /// <summary>
    /// 删除商家命令
    /// </summary>
    public record DeleteMerchantCommand(Guid Id) : IRequest;
}
