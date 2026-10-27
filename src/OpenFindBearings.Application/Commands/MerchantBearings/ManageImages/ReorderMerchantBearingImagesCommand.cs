using MediatR;

namespace OpenFindBearings.Application.Commands.MerchantBearings.ManageImages
{
    /// <summary>
    /// 重排商家商品实拍图命令（v2.22.0）：首项即首图（商城与商品卡片展示），
    /// 传入地址集合必须与现有图片完全一致；已通过审核的商品改序后需要重新送审。
    /// </summary>
    public record ReorderMerchantBearingImagesCommand : IRequest<string[]>
    {
        /// <summary>商家商品关联ID</summary>
        public Guid Id { get; init; }

        /// <summary>操作人用户ID（须为该商户在职成员）</summary>
        public Guid UserId { get; init; }

        /// <summary>按目标顺序排列的图片地址</summary>
        public List<string> Urls { get; init; } = [];
    }
}
