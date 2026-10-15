using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Application.Extensions
{
    public static class MerchantBearingExtensions
    {
        /// <summary>
        /// 商家-轴承关联 → 公共 DTO（v1.6.0 数据可信联动重构）。
        /// 改动说明：合并原 ToPublicDto/ToAuthenticatedDto 两个同输出方法（isAuthenticated 参数
        ///   最终仅用于屏蔽联系方式，现已由"电话单轨"整体移除联系方式，故参数与方法一并删除）；
        ///   新增 BearingDataSource 下发，供前端对参数标注"仅供参考"。
        /// </summary>
        public static MerchantBearingDto ToDto(this MerchantBearing mb)
        {
            return new MerchantBearingDto
            {
                Id = mb.Id,
                MerchantId = mb.MerchantId,
                MerchantName = mb.Merchant?.Name,
                MerchantGrade = mb.Merchant?.Grade.ToString(),
                MerchantCity = mb.Merchant?.Contact?.City,
                IsPriceVisible = mb.IsPriceVisible(true),

                // 轴承参数来源（可信标注数据源）：Internet/OcrExtract/SeedData=仅供参考
                BearingDataSource = mb.Bearing?.DataSource?.SourceType.ToString(),

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
    }
}