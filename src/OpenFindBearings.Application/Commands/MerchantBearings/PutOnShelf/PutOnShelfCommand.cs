using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.MerchantBearings.PutOnShelf
{
    /// <summary>
    /// 上架产品命令
    /// </summary>
    public record PutOnShelfCommand : IRequest, ICommand
    {
        /// <summary>
        /// 关联ID
        /// </summary>
        public Guid MerchantBearingId { get; init; }
    }
}
