namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 商城商品详情里的商家摘要（v1.59.16）：商品详情页底部「店铺」入口与商家标识所需。
    /// 只给公开展示字段，不含邮箱等敏感信息。
    /// </summary>
    public record MallItemMerchantDto(
        Guid Id,
        string Name,
        string? LogoUrl,
        string? Grade,
        bool IsVerified,
        string? Phone);

    /// <summary>
    /// 商城商品详情（v1.59.16）：商家 x 商品维度的单品详情。
    /// Item 复用货源流卡片 DTO（型号/品牌/尺寸/价格/库存/商家实拍图），
    /// 另补商家摘要与型号/品牌描述，供商品详情页展示。
    /// </summary>
    public record MallItemDetailDto(
        MerchantBearingDto Item,
        MallItemMerchantDto Merchant,
        string? BearingDescription,
        string? BrandDescription);
}