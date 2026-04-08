using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Application.Commands.MerchantBearings.SetPriceVisibility
{
    /// <summary>
    /// 设置价格可见性命令
    /// </summary>
    public record SetPriceVisibilityCommand : IRequest, ICommand
    {
        /// <summary>
        /// 商家产品关联ID
        /// </summary>
        public Guid MerchantBearingId { get; init; }

        /// <summary>
        /// 价格可见性
        /// </summary>
        public PriceVisibility Visibility { get; init; }
    }
}
