using MediatR;

namespace OpenFindBearings.Application.Features.MerchantBearings.Commands
{
    /// <summary>
    /// 上架产品命令
    /// </summary>
    public record PutOnShelfCommand(Guid MerchantBearingId) : IRequest;
}
