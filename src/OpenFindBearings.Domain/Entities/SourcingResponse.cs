using OpenFindBearings.Domain.Abstractions;
using OpenFindBearings.Domain.Enums;

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

        /// <summary>所属寻货需求</summary>
        public Guid DemandId { get; private set; }

        /// <summary>应答商户 ID（归属主体）</summary>
        public Guid MerchantId { get; private set; }

        /// <summary>操作人用户 ID（审计：商户内哪个成员提交的应答）</summary>
        public Guid RespondedUserId { get; private set; }

        /// <summary>应答说明（必填一句话：货源来源/成色/可否验货等）</summary>
        public string Remark { get; private set; } = string.Empty;

        /// <summary>状态（SourcingResponseStatus 枚举，v2.15.0）</summary>
        public SourcingResponseStatus Status { get; private set; } = SourcingResponseStatus.Pending;

        /// <summary>应答型号行（v1.5.0 多行标书：发布人挑选的依据，每行可引用在售商品）</summary>
        public List<SourcingResponseItem> Items { get; private set; } = new();

        /// <summary>被选定时刻（UTC；v2.18.0 撮合成交链，供成交回执窗口与审计）</summary>
        public DateTime? AdoptedAt { get; private set; }

        /// <summary>成交回执结果（null=需求方未回填，中性不计分；v2.18.0）</summary>
        public SourcingDealOutcome? FeedbackOutcome { get; private set; }

        /// <summary>回执补充说明（可选，如"其他"原因的文字描述）</summary>
        public string? FeedbackReason { get; private set; }

        /// <summary>回执提交时刻（UTC）</summary>
        public DateTime? FeedbackAt { get; private set; }

        /// <summary>
        /// 该回执是否计入商家卡级（v2.18.0 反刷）：仅"谈成 + 通过自关联/资格/异常校验"才为 true；
        /// 管理员撤销回执亦翻转为 false（留痕不改结果，便于审计）
        /// </summary>
        public bool IsCounted { get; private set; }

        /// <summary>不计分留痕说明（自关联/资格不足/异常检测命中，供 Admin 复核）</summary>
        public string? FlagNote { get; private set; }

        /// <summary>回执提交来源 IP（v2.18.0 反刷：同 IP 短时集中提交用于异常检测，仅内部留痕）</summary>
        public string? FeedbackIp { get; private set; }

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
                Status = SourcingResponseStatus.Pending,
            };
        }

        /// <summary>
        /// 被发布人选定（v2.18.0：同时记录选定时刻，作为成交回执的窗口锚点）
        /// </summary>
        public void Adopt()
        {
            Status = SourcingResponseStatus.Adopted;
            AdoptedAt ??= DateTime.UtcNow;
        }

        /// <summary>
        /// 提交/更新成交回执（v2.18.0 撮合成交链，仅需求方本人可调，由端点校验身份）：
        /// 只有 Confirmed 且校验通过时 isCounted 才为 true，进入商家卡级统计
        /// </summary>
        /// <param name="outcome">洽谈结果</param>
        /// <param name="reason">原因/补充说明（可选）</param>
        /// <param name="isCounted">是否计入卡级（反刷校验结论）</param>
        /// <param name="flagNote">不计分留痕说明（可选）</param>
        /// <param name="ip">提交来源 IP（可选，异常检测留痕）</param>
        public void SubmitFeedback(SourcingDealOutcome outcome, string? reason, bool isCounted, string? flagNote,
            string? ip = null)
        {
            FeedbackOutcome = outcome;
            FeedbackReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
            IsCounted = isCounted;
            FlagNote = string.IsNullOrWhiteSpace(flagNote) ? null : flagNote.Trim();
            FeedbackIp = string.IsNullOrWhiteSpace(ip) ? null : ip.Trim();
            FeedbackAt = DateTime.UtcNow;
            UpdateTimestamp();
        }

        /// <summary>
        /// 翻转该回执的计分标记（v2.18.0 管理侧复核/撤销）：不影响已记录的回执结果，只决定是否参与卡级统计
        /// </summary>
        /// <param name="counted">是否计分</param>
        /// <param name="note">复核说明（写入留痕）</param>
        public void SetFeedbackCounted(bool counted, string? note)
        {
            IsCounted = counted;
            if (!string.IsNullOrWhiteSpace(note))
                FlagNote = note.Trim();
            UpdateTimestamp();
        }

        /// <summary>需求关闭但未被选定</summary>
        public void MarkNotSelected() => Status = SourcingResponseStatus.NotSelected;

        /// <summary>
        /// 撤销应答（v1.8.2 软取消）：仅待处理态可撤，改为置 Cancelled 而非删行——
        /// 行保留以满足"是否已应答"排除判定与审计，同时因行仍存在使当日额度/三件套应答项不回退
        /// </summary>
        /// <exception cref="InvalidOperationException">非待处理态（已被选定/已落选/已撤销）</exception>
        public void Cancel()
        {
            if (Status != SourcingResponseStatus.Pending)
                throw new InvalidOperationException("该应答已被处理，无法撤销");
            Status = SourcingResponseStatus.Cancelled;
        }

        /// <summary>
        /// 撤销后重新应答（v1.8.2 软取消配套）：已撤销的行复用而非新插一行——
        /// 复用行既守住 (DemandId, MerchantId) 唯一约束，又天然不重复占额度/不重复扣分
        /// </summary>
        /// <param name="remark">新的应答说明</param>
        /// <exception cref="InvalidOperationException">非已撤销态（该方法是撤销回退专用）</exception>
        public void Revive(string remark)
        {
            if (Status != SourcingResponseStatus.Cancelled)
                throw new InvalidOperationException("仅已撤销的应答可以重新提交");
            Status = SourcingResponseStatus.Pending;
            Remark = remark.Trim();
        }

        /// <summary>更新应答说明（同需求重复应答=覆盖更新，仅待处理态可改；型号行由调用方整体替换）</summary>
        /// <param name="remark">应答说明</param>
        public void UpdateContent(string remark)
        {
            if (Status != SourcingResponseStatus.Pending)
                throw new InvalidOperationException("仅待处理的应答可以更新");
            Remark = remark.Trim();
        }
    }
}
