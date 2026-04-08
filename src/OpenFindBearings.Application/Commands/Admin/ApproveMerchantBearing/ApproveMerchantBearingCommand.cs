using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Admin.ApproveMerchantBearing
{
    /// <summary>
    /// 审核通过商家产品命令
    /// </summary>
    public record ApproveMerchantBearingCommand : IRequest, ICommand
    {
        /// <summary>
        /// 商家产品关联ID
        /// </summary>
        public Guid MerchantBearingId { get; init; }

        /// <summary>
        /// 审核人ID
        /// </summary>
        public Guid ReviewedBy { get; init; }
    }
}
