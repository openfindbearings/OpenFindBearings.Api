using MediatR;

namespace OpenFindBearings.Application.Features.History.Commands
{
    /// <summary>
    /// 记录商家浏览历史命令
    /// </summary>
    public record RecordMerchantViewCommand : IRequest
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// 商家ID
        /// </summary>
        public Guid MerchantId { get; init; }
    }
}
