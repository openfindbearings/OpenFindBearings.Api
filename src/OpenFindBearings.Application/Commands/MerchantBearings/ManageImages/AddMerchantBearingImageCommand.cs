using MediatR;

namespace OpenFindBearings.Application.Commands.MerchantBearings.ManageImages
{
    /// <summary>
    /// 追加商家商品实拍图命令（v2.22.0）：图片只属于本商家的这件商品，
    /// 不写入轴承主数据图；已通过审核的商品改图后需要重新送审。
    /// </summary>
    public record AddMerchantBearingImageCommand : IRequest<string[]>
    {
        /// <summary>商家商品关联ID</summary>
        public Guid Id { get; init; }

        /// <summary>操作人用户ID（须为该商户在职成员）</summary>
        public Guid UserId { get; init; }

        /// <summary>图片字节</summary>
        public byte[] Content { get; init; } = [];

        /// <summary>图片扩展名（含点，小写）</summary>
        public string? Extension { get; init; }
    }
}
