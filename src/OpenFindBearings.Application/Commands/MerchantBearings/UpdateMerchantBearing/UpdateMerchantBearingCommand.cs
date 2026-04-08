using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.MerchantBearings.UpdateMerchantBearing
{
    /// <summary>
    /// 更新商家-轴承关联命令
    /// </summary>
    public record UpdateMerchantBearingCommand : IRequest, ICommand
    {
        /// <summary>
        /// 关联ID
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// 价格描述
        /// </summary>
        public string? PriceDescription { get; init; }

        /// <summary>
        /// 库存描述
        /// </summary>
        public string? StockDescription { get; init; }

        /// <summary>
        /// 最小起订量描述
        /// </summary>
        public string? MinOrderDescription { get; init; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remarks { get; init; }
    }
}
