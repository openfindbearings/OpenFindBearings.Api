using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.MerchantBearings.TakeOffShelf
{
    /// <summary>
    /// 下架产品命令
    /// </summary>
    public record TakeOffShelfCommand : IRequest, ICommand
    {
        /// <summary>
        /// 关联ID
        /// </summary>
        public Guid MerchantBearingId { get; init; }
    }
}
