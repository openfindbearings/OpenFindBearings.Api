using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Admin.RejectMerchantBearing
{
    /// <summary>
    /// 拒绝商家产品命令
    /// </summary>
    public record RejectMerchantBearingCommand : IRequest, ICommand
    {
        /// <summary>
        /// 商家产品关联ID
        /// </summary>
        public Guid MerchantBearingId { get; init; }

        /// <summary>
        /// 审核人ID
        /// </summary>
        public Guid ReviewedBy { get; init; }

        /// <summary>
        /// 拒绝理由
        /// </summary>
        public string Reason { get; init; } = string.Empty;
    }
}
