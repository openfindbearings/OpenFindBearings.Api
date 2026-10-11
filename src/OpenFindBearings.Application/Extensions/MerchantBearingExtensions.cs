using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Application.Extensions
{
    public static class MerchantBearingExtensions
    {
        public static MerchantBearingDto ToPublicDto(this MerchantBearing mb)
        {
            return new MerchantBearingDto
            {
                Id = mb.Id,
                MerchantId = mb.MerchantId,

                // ========== 敏感信息控制(匿名用户看不到) ==========
                //MerchantName = null,
                //MerchantGrade = string.Empty,
                //MerchantCity = mb.Merchant?.Contact?.City,
                //MerchantPhone = null,
                //MerchantAddress = null,
                //IsPriceVisible = mb.IsPriceVisible(false),

                // 还是放开，全能查到，因为这些信息目前是来源于网络，准确性不保证
                MerchantName = mb.Merchant?.Name,
                MerchantGrade = mb.Merchant?.Grade.ToString(),
                MerchantCity = mb.Merchant?.Contact?.City,
                MerchantPhone = mb.Merchant?.Contact?.Phone,
                MerchantAddress = mb.Merchant?.Contact?.Address,
                IsPriceVisible = mb.IsPriceVisible(true),

                // ========== 其他字段 ==========
                MerchantIsVerified = mb.Merchant?.IsVerified ?? false,
                BearingId = mb.BearingId,
                BearingPartNumber = mb.Bearing?.PartNumber ?? string.Empty,
                BearingOldNumber = mb.Bearing?.OldNumber,
                BearingTypeName = mb.Bearing?.BearingType,
                BrandName = mb.Bearing?.Brand?.Name,
                BrandLevel = mb.Bearing?.Brand?.Level.ToString(),
                Dimensions = mb.Bearing != null
                        ? $"{mb.Bearing.Dimensions.InnerDiameter}×{mb.Bearing.Dimensions.OuterDiameter}×{mb.Bearing.Dimensions.Width}"
                        : null,
                // 改动说明（v2.14.0 商城导购）：商品卡需主图，从关联轴承带出（匿名也可见，图为公开信息）
                Image3DUrl = mb.Bearing?.Image3DUrl,
                Image2DUrl = mb.Bearing?.Image2DUrl,
                PriceDescription = mb.PriceDescription,
                PriceVisibility = mb.PriceVisibility,
                NumericPrice = mb.NumericPrice,
                StockDescription = mb.StockDescription,
                MinOrderDescription = mb.MinOrderDescription,
                Remarks = mb.Remarks,
                // v2.15.0 状态统一：SaleState + ReviewStatus
                SaleState = mb.SaleState,
                RestockEta = mb.RestockEta,
                ReviewStatus = mb.ReviewStatus,
                ApprovalComment = mb.ApprovalComment,
                ViewCount = mb.ViewCount,
                CreatedAt = mb.CreatedAt,
                UpdatedAt = mb.UpdatedAt               
            };
        }

        public static MerchantBearingDto ToAuthenticatedDto(this MerchantBearing mb)
        {
            return new MerchantBearingDto
            {
                Id = mb.Id,
                MerchantId = mb.MerchantId,

                ////////// ========== 敏感信息控制(匿名用户看不到) ==========  目前不处理，后期关联了注册了的员工
                MerchantName = mb.Merchant?.Name,
                MerchantGrade = mb.Merchant?.Grade.ToString(),
                MerchantCity = mb.Merchant?.Contact?.City,
                MerchantPhone = mb.Merchant?.Contact?.Phone,
                MerchantAddress = mb.Merchant?.Contact?.Address,
                IsPriceVisible = mb.IsPriceVisible(true),

                // ========== 其他字段 ==========
                MerchantIsVerified = mb.Merchant?.IsVerified ?? false,
                BearingId = mb.BearingId,
                BearingPartNumber = mb.Bearing?.PartNumber ?? string.Empty,
                BearingOldNumber = mb.Bearing?.OldNumber,
                BearingTypeName = mb.Bearing?.BearingType,
                BrandName = mb.Bearing?.Brand?.Name,
                BrandLevel = mb.Bearing?.Brand?.Level.ToString(),
                Dimensions = mb.Bearing != null
                        ? $"{mb.Bearing.Dimensions.InnerDiameter}×{mb.Bearing.Dimensions.OuterDiameter}×{mb.Bearing.Dimensions.Width}"
                        : null,
                // 改动说明（v2.14.0 商城导购）：商品卡需主图，从关联轴承带出
                Image3DUrl = mb.Bearing?.Image3DUrl,
                Image2DUrl = mb.Bearing?.Image2DUrl,
                PriceDescription = mb.PriceDescription,
                PriceVisibility = mb.PriceVisibility,
                NumericPrice = mb.NumericPrice,
                StockDescription = mb.StockDescription,
                MinOrderDescription = mb.MinOrderDescription,
                Remarks = mb.Remarks,
                // v2.15.0 状态统一：SaleState + ReviewStatus
                SaleState = mb.SaleState,
                RestockEta = mb.RestockEta,
                ReviewStatus = mb.ReviewStatus,
                ApprovalComment = mb.ApprovalComment,
                ViewCount = mb.ViewCount,
                CreatedAt = mb.CreatedAt,
                UpdatedAt = mb.UpdatedAt                
            };
        }

        public static MerchantBearingDto ToDto(this MerchantBearing mb, bool isAuthenticated)
        {
            return isAuthenticated ? mb.ToAuthenticatedDto() : mb.ToPublicDto();
        }
    }
}
