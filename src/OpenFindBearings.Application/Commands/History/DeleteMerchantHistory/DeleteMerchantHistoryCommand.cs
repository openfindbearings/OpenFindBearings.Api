using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.History.DeleteMerchantHistory
{
    /// <summary>
    /// 删除单条商家浏览历史命令（按 userId+merchantId 定位，防越权删他人记录）
    /// </summary>
    public record DeleteMerchantHistoryCommand : IRequest, ICommand
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// 商家ID（历史行按用户+商家唯一定位）
        /// </summary>
        public Guid MerchantId { get; init; }
    }
}
