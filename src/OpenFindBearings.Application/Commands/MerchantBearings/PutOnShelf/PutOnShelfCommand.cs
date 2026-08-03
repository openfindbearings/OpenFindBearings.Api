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

        /// <summary>
        /// 当前用户ID（API 端点注入，用于所有权验证）
        /// </summary>
        public Guid UserId { get; init; }
    }
}
