using MediatR;

namespace OpenFindBearings.Application.Features.MerchantBearings.Commands
{
    /// <summary>
    /// 下架产品命令
    /// </summary>
    public record TakeOffShelfCommand(Guid MerchantBearingId) : IRequest;
}
