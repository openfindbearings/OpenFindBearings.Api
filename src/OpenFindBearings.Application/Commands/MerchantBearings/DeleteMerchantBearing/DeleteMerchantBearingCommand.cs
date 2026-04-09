using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.MerchantBearings.DeleteMerchantBearing
{
    /// <summary>
    /// 删除商家-轴承关联命令
    /// </summary>
    public record DeleteMerchantBearingCommand : IRequest, ICommand
    {
        /// <summary>
        /// 关联ID
        /// </summary>
        public Guid Id { get; init; }
    }
}
