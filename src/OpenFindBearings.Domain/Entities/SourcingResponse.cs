using OpenFindBearings.Domain.Abstractions;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 寻货应答实体（v1.35.0，SourcingDemand 聚合内）：商户对寻货需求的报价应答。
    /// 归属判定是商户（MerchantId）而非操作人——应答是商家行为，操作人 userId 仅留审计列；
    /// 需求关闭时未选中的应答批量转 NotSelected
    /// </summary>
    public class SourcingResponse : BaseEntity
    {
        /// <summary>状态：待发布人处理</summary>
        public const int StatusPending = 1;
        /// <summary>状态：被选定（发布人确认合作，解锁双方联系方式）</summary>
        public const int StatusAdopted = 2;
        /// <summary>状态：未选中（需求关闭时其余应答流转至此）</summary>
        public const int StatusNotSelected = 3;

        /// <summary>所属寻货需求</summary>
        public Guid DemandId { get; private set; }

        /// <summary>应答商户 ID（归属主体）</summary>
        public Guid MerchantId { get; private set; }

        /// <summary>操作人用户 ID（审计：商户内哪个成员提交的应答）</summary>
        public Guid RespondedUserId { get; private set; }

        /// <summary>报价单价（可选——B2B 常"电话聊"，必填项太多应答率会死）</summary>
        public decimal? Price { get; private set; }

        /// <summary>库存描述（可选）</summary>
        public string? Stock { get; private set; }

        /// <summary>交期描述（可选）</summary>
        public string? LeadTime { get; private set; }

        /// <summary>应答说明（必填一句话：货源来源/成色/可否验货等）</summary>
        public string Remark { get; private set; } = string.Empty;

        /// <summary>状态（Status* 常量）</summary>
        public int Status { get; private set; } = StatusPending;

        /// <summary>EF 专用无参构造</summary>
        protected SourcingResponse() { }

        /// <summary>
        /// 工厂：创建应答（待处理态）
        /// </summary>
        /// <param name="demandId">寻货需求</param>
        /// <param name="merchantId">应答商户</param>
        /// <param name="userId">操作人（审计）</param>
        /// <param name="price">报价</param>
        /// <param name="stock">库存描述</param>
        /// <param name="leadTime">交期描述</param>
        /// <param name="remark">应答说明（必填）</param>
        public static SourcingResponse Create(Guid demandId, Guid merchantId, Guid userId,
            decimal? price, string? stock, string? leadTime, string remark)
        {
            return new SourcingResponse
            {
                DemandId = demandId,
                MerchantId = merchantId,
                RespondedUserId = userId,
                Price = price,
                Stock = string.IsNullOrWhiteSpace(stock) ? null : stock.Trim(),
                LeadTime = string.IsNullOrWhiteSpace(leadTime) ? null : leadTime.Trim(),
                Remark = remark.Trim(),
                Status = StatusPending,
            };
        }

        /// <summary>被发布人选定</summary>
        public void Adopt() => Status = StatusAdopted;

        /// <summary>需求关闭但未被选定</summary>
        public void MarkNotSelected() => Status = StatusNotSelected;

        /// <summary>更新应答内容（同需求重复应答=覆盖更新，仅待处理态可改）</summary>
        /// <param name="price">报价</param>
        /// <param name="stock">库存描述</param>
        /// <param name="leadTime">交期描述</param>
        /// <param name="remark">应答说明</param>
        public void UpdateContent(decimal? price, string? stock, string? leadTime, string remark)
        {
            if (Status != StatusPending)
                throw new InvalidOperationException("仅待处理的应答可以更新");
            Price = price;
            Stock = string.IsNullOrWhiteSpace(stock) ? null : stock.Trim();
            LeadTime = string.IsNullOrWhiteSpace(leadTime) ? null : leadTime.Trim();
            Remark = remark.Trim();
        }
    }
}
