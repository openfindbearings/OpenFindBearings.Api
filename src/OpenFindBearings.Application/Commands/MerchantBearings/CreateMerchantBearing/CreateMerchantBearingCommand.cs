using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.MerchantBearings.Commands
{
    /// <summary>
    /// 创建商家-轴承关联命令
    /// </summary>
    public record CreateMerchantBearingCommand : IRequest<Guid>, ICommand
    {
        /// <summary>
        /// 商家ID
        /// </summary>
        public Guid MerchantId { get; init; }

        /// <summary>
        /// 轴承ID
        /// </summary>
        public Guid BearingId { get; init; }

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
