using MediatR;

namespace OpenFindBearings.Application.Commands.Bearings.UpdateBearingImages
{
    /// <summary>
    /// 更新轴承图片命令（v2.21.3）：运营后台直接维护型号主图，不走纠错审核。
    /// 型号主图属平台资产（用于认型号），同一型号的商家实拍图另由商家商品资料维护，
    /// 因此这里只接受管理员操作，且每次只处理 3D/2D 两个固定槽位。
    /// </summary>
    public record UpdateBearingImagesCommand : IRequest<UpdateBearingImagesResult>
    {
        /// <summary>轴承ID</summary>
        public Guid Id { get; init; }

        /// <summary>3D 图文件字节；为空表示不替换该槽位</summary>
        public byte[]? Image3D { get; init; }

        /// <summary>3D 图文件扩展名（含点，小写）</summary>
        public string? Image3DExtension { get; init; }

        /// <summary>2D 图文件字节；为空表示不替换该槽位</summary>
        public byte[]? Image2D { get; init; }

        /// <summary>2D 图文件扩展名（含点，小写）</summary>
        public string? Image2DExtension { get; init; }

        /// <summary>是否清空 3D 图（提供新文件时以新文件为准）</summary>
        public bool ClearImage3D { get; init; }

        /// <summary>是否清空 2D 图（提供新文件时以新文件为准）</summary>
        public bool ClearImage2D { get; init; }
    }

    /// <summary>
    /// 更新轴承图片结果：返回两个槽位更新后的 URL（清空为 null）
    /// </summary>
    public record UpdateBearingImagesResult(string? Image3DUrl, string? Image2DUrl);
}
