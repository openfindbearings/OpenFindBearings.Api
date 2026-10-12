using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.MerchantBearings.RemoveMerchantBearing
{
    /// <summary>
    /// 删除商家商品命令（v2.15.0 商家删除）。
    /// 策略：出售中/补货中禁删（须先下架）；仓库中且从未置顶的硬删；其余非公域态软删。全体成员可删。
    /// </summary>
    public record RemoveMerchantBearingCommand : IRequest, ICommand
    {
        /// <summary>商家-轴承关联ID</summary>
        public Guid MerchantBearingId { get; init; }

        /// <summary>当前用户ID（API 端点注入，用于归属校验）</summary>
        public Guid UserId { get; init; }
    }
}