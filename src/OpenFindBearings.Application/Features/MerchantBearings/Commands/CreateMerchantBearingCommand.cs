using MediatR;

namespace OpenFindBearings.Application.Features.MerchantBearings.Commands
{
    /// <summary>
    /// 创建商家-轴承关联命令
    /// </summary>
    public record CreateMerchantBearingCommand : IRequest<Guid>
    {
        public Guid MerchantId { get; init; }
        public Guid BearingId { get; init; }
        public string? PriceDescription { get; init; }
        public string? StockDescription { get; init; }
        public string? MinOrderDescription { get; init; }
        public string? Remarks { get; init; }
    }
}
