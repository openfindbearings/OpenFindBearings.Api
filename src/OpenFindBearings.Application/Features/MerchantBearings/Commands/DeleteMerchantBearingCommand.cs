using MediatR;

namespace OpenFindBearings.Application.Features.MerchantBearings.Commands
{
    /// <summary>
    /// 删除商家-轴承关联命令
    /// </summary>
    public record DeleteMerchantBearingCommand : IRequest
    {
        /// <summary>
        /// 关联ID
        /// </summary>
        public Guid Id { get; init; }
    }
}
