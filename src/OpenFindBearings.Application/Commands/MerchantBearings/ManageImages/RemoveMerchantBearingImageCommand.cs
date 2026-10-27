using MediatR;

namespace OpenFindBearings.Application.Commands.MerchantBearings.ManageImages
{
    /// <summary>
    /// 删除商家商品实拍图命令（v2.22.0）：按图片地址删除，
    /// 已通过审核的商品删图后需要重新送审。
    /// </summary>
    public record RemoveMerchantBearingImageCommand : IRequest<string[]>
    {
        /// <summary>商家商品关联ID</summary>
        public Guid Id { get; init; }

        /// <summary>操作人用户ID（须为该商户在职成员）</summary>
        public Guid UserId { get; init; }

        /// <summary>要删除的图片地址</summary>
        public string Url { get; init; } = string.Empty;
    }
}
