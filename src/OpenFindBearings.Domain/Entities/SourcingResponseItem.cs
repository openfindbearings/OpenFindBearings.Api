using OpenFindBearings.Domain.Abstractions;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 寻货应答型号行实体（v1.5.0 多行标书）：一条应答（商家一份标书）含多行型号。
    /// 发布人需求明确时商家填一行（通常引用在售商品）；需求模糊时商家把自己认为相关的
    /// 型号挨个列成多行，供发布人按需挑选——选定粒度仍是整条应答（商家），行是给发布人的依据
    /// </summary>
    public class SourcingResponseItem : BaseEntity
    {
        /// <summary>所属应答</summary>
        public Guid ResponseId { get; private set; }

        /// <summary>型号（必填；平台库外的自由文本型号也可）</summary>
        public string PartNumber { get; private set; } = string.Empty;

        /// <summary>引用的在售商品 ID（MerchantBearing，可空=自由文本型号行）</summary>
        public Guid? BearingId { get; private set; }

        /// <summary>该行报价单价（可选）</summary>
        public decimal? Price { get; private set; }

        /// <summary>该行库存描述（可选）</summary>
        public string? Stock { get; private set; }

        /// <summary>该行交期描述（可选）</summary>
        public string? LeadTime { get; private set; }

        /// <summary>EF 专用无参构造</summary>
        protected SourcingResponseItem() { }

        /// <summary>
        /// 工厂：创建应答型号行
        /// </summary>
        /// <param name="responseId">所属应答</param>
        /// <param name="partNumber">型号（必填）</param>
        /// <param name="bearingId">引用的在售商品（可空）</param>
        /// <param name="price">报价</param>
        /// <param name="stock">库存描述</param>
        /// <param name="leadTime">交期描述</param>
        public static SourcingResponseItem Create(Guid responseId, string partNumber, Guid? bearingId,
            decimal? price, string? stock, string? leadTime)
        {
            return new SourcingResponseItem
            {
                ResponseId = responseId,
                PartNumber = partNumber.Trim(),
                BearingId = bearingId,
                Price = price,
                Stock = string.IsNullOrWhiteSpace(stock) ? null : stock.Trim(),
                LeadTime = string.IsNullOrWhiteSpace(leadTime) ? null : leadTime.Trim(),
            };
        }
    }
}
