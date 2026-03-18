using MediatR;

namespace OpenFindBearings.Application.Features.MerchantBearings.Commands
{
    /// <summary>
    /// 更新商家-轴承关联命令
    /// </summary>
    public record UpdateMerchantBearingCommand : IRequest
    {
        public Guid Id { get; set; }
        public string? PriceDescription { get; init; }
        public string? StockDescription { get; init; }
        public string? MinOrderDescription { get; init; }
        public string? Remarks { get; init; }
    }
}
