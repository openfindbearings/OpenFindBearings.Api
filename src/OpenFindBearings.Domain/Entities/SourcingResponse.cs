using OpenFindBearings.Domain.Abstractions;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 寻货应答实体（v1.35.0，SourcingDemand 聚合内）：商户对寻货需求的报价应答。
    /// 归属判定是商户（MerchantId）而非操作人——应答是商家行为，操作人 userId 仅留审计列；
    /// 需求关闭时未选中的应答批量转 NotSelected。
    /// v1.5.0 多行标书：报价/库存/交期字段下沉到 SourcingResponseItem（每行型号各自携带），
    /// 主表仅保留整份标书的说明 Remark；选定粒度仍是整条应答（商家），行是发布人选定的依据
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

        /// <summary>应答说明（必填一句话：货源来源/成色/可否验货等）</summary>
        public string Remark { get; private set; } = string.Empty;

        /// <summary>状态（Status* 常量）</summary>
        public int Status { get; private set; } = StatusPending;

        /// <summary>应答型号行（v1.5.0 多行标书：发布人挑选的依据，每行可引用在售商品）</summary>
        public List<SourcingResponseItem> Items { get; private set; } = new();

        /// <summary>EF 专用无参构造</summary>
        protected SourcingResponse() { }

        /// <summary>
        /// 工厂：创建应答（待处理态），型号行由调用方追加到 Items
        /// </summary>
        /// <param name="demandId">寻货需求</param>
        /// <param name="merchantId">应答商户</param>
        /// <param name="userId">操作人（审计）</param>
        /// <param name="remark">应答说明（必填）</param>
        public static SourcingResponse Create(Guid demandId, Guid merchantId, Guid userId, string remark)
        {
            return new SourcingResponse
            {
                DemandId = demandId,
                MerchantId = merchantId,
                RespondedUserId = userId,
                Remark = remark.Trim(),
                Status = StatusPending,
            };
        }

        /// <summary>被发布人选定</summary>
        public void Adopt() => Status = StatusAdopted;

        /// <summary>需求关闭但未被选定</summary>
        public void MarkNotSelected() => Status = StatusNotSelected;

        /// <summary>更新应答说明（同需求重复应答=覆盖更新，仅待处理态可改；型号行由调用方整体替换）</summary>
        /// <param name="remark">应答说明</param>
        public void UpdateContent(string remark)
        {
            if (Status != StatusPending)
                throw new InvalidOperationException("仅待处理的应答可以更新");
            Remark = remark.Trim();
        }
    }
}
