using OpenFindBearings.Domain.Abstractions;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.ValueObjects;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 轴承替代品关系实体
    /// 表示不同轴承型号之间的替代关系，如 SKF 6205 可以被 NSK 6205 替代
    /// 用于为用户提供替代品推荐功能
    /// 对应接口：GET /api/bearings/{id}/interchanges
    /// </summary>
    public class BearingInterchange : BaseEntity
    {
        /// <summary>
        /// 源轴承ID
        /// 要被替代的原始轴承
        /// </summary>
        public Guid SourceBearingId { get; private set; }

        /// <summary>
        /// 源轴承导航属性
        /// </summary>
        public Bearing? SourceBearing { get; private set; }

        /// <summary>
        /// 目标轴承ID
        /// 可以作为替代品的轴承
        /// </summary>
        public Guid TargetBearingId { get; private set; }

        /// <summary>
        /// 目标轴承导航属性
        /// </summary>
        public Bearing? TargetBearing { get; private set; }

        /// <summary>
        /// 替代类型
        /// exact: 完全替代（尺寸、性能完全相同）
        /// conditional: 有条件替代（尺寸相近，但可能需要调整）
        /// functional: 功能替代（相同功能但尺寸不同）
        /// </summary>
        public string? InterchangeType { get; private set; }

        /// <summary>
        /// 可信度（1-100）
        /// 表示这个替代关系的可靠程度，数值越高越可信
        /// 来源权威、数据准确则可信度高
        /// </summary>
        public int Confidence { get; private set; }

        /// <summary>
        /// 数据来源
        /// 如 "SKF官方互换表"、"国标互换手册"、"网络收集" 等
        /// 用于追溯数据来源和评估可信度
        /// </summary>
        public string? Source { get; private set; }

        /// <summary>
        /// 备注信息
        /// 用于记录特殊说明，如"需要更换密封圈"等
        /// </summary>
        public string? Remarks { get; private set; }

        /// <summary>
        /// 是否双向替代
        /// true: 替代关系可逆（A可替B，B也可替A）
        /// false: 单向替代（仅A可替B）
        /// </summary>
        public bool IsBidirectional { get; private set; }

        /// <summary>
        /// 数据来源（覆盖保护用）
        /// 人工维护/审核过的替代品来源标记为非互联网，互联网批量同步不再覆盖
        /// </summary>
        public DataSource? DataSource { get; private set; }

        /// <summary>
        /// 审核状态（v2.16.0 替代品治理）：平台自建/审核通过=已生效；商家提交=待审核；驳回=已驳回。
        /// 仅"已生效"对公域可见
        /// </summary>
        public InterchangeReviewStatus ReviewStatus { get; private set; } = InterchangeReviewStatus.Approved;

        /// <summary>
        /// 驳回理由（v2.16.0）：仅 Rejected 时有值，提交方可改后重提
        /// </summary>
        public string? ReviewComment { get; private set; }

        /// <summary>
        /// 提出该候选替代的商户ID（v2.16.0）：商家提交时记录，供追溯与责任/黑名单；平台自建为 null
        /// </summary>
        public Guid? ProposedByMerchantId { get; private set; }

        /// <summary>
        /// 无参构造函数，仅供EF Core使用
        /// </summary>
        private BearingInterchange() { }

        /// <summary>
        /// 创建轴承替代关系
        /// </summary>
        /// <param name="sourceBearingId">源轴承ID</param>
        /// <param name="targetBearingId">目标轴承ID</param>
        /// <param name="interchangeType">替代类型（exact/conditional/functional）</param>
        /// <param name="confidence">可信度 1-100</param>
        /// <param name="source">数据来源</param>
        /// <param name="remarks">备注信息</param>
        /// <param name="isBidirectional">是否双向替代</param>
        /// <exception cref="ArgumentException">参数验证异常</exception>
        public BearingInterchange(
            Guid sourceBearingId,
            Guid targetBearingId,
            string? interchangeType = "exact",
            int confidence = 80,
            string? source = null,
            string? remarks = null,
            bool isBidirectional = true)
        {
            if (sourceBearingId == Guid.Empty)
                throw new ArgumentException("源轴承ID不能为空", nameof(sourceBearingId));
            if (targetBearingId == Guid.Empty)
                throw new ArgumentException("目标轴承ID不能为空", nameof(targetBearingId));
            if (sourceBearingId == targetBearingId)
                throw new ArgumentException("源轴承和目标轴承不能相同", nameof(targetBearingId));
            if (confidence < 0 || confidence > 100)
                throw new ArgumentException("可信度必须在1-100之间", nameof(confidence));

            Id = Guid.NewGuid();
            SourceBearingId = sourceBearingId;
            TargetBearingId = targetBearingId;
            InterchangeType = interchangeType;
            Confidence = confidence;
            Source = source;
            Remarks = remarks;
            IsBidirectional = isBidirectional;
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 更新可信度
        /// 当有更多数据验证后，可以调整可信度
        /// </summary>
        /// <param name="newConfidence">新的可信度</param>
        /// <exception cref="ArgumentException">参数验证异常</exception>
        public void UpdateConfidence(int newConfidence)
        {
            if (newConfidence < 0 || newConfidence > 100)
                throw new ArgumentException("可信度必须在1-100之间", nameof(newConfidence));

            Confidence = newConfidence;
            UpdateTimestamp();
        }

        /// <summary>
        /// 更新备注信息
        /// </summary>
        /// <param name="remarks">新的备注</param>
        public void UpdateRemarks(string? remarks)
        {
            Remarks = remarks;
            UpdateTimestamp();
        }

        /// <summary>
        /// 设置数据来源
        /// </summary>
        public void SetDataSource(DataSource dataSource)
        {
            DataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            UpdateTimestamp();
        }

        /// <summary>
        /// 更新替代明细（平台编辑：类型/可信度/来源/备注/双向）
        /// </summary>
        public void UpdateDetails(string? interchangeType, int confidence, string? source, string? remarks, bool isBidirectional)
        {
            if (confidence < 0 || confidence > 100)
                throw new ArgumentException("可信度必须在0-100之间", nameof(confidence));

            InterchangeType = interchangeType;
            Confidence = confidence;
            Source = source;
            Remarks = remarks;
            IsBidirectional = isBidirectional;
            UpdateTimestamp();
        }

        /// <summary>平台自建/审核通过标记（v2.16.0）：置为已生效并清空驳回理由</summary>
        public void MarkApproved()
        {
            ReviewStatus = InterchangeReviewStatus.Approved;
            ReviewComment = null;
            UpdateTimestamp();
        }

        /// <summary>商家提交候选（v2.16.0）：置待审核并记录提出商户</summary>
        public void SubmitByMerchant(Guid merchantId)
        {
            if (merchantId == Guid.Empty)
                throw new ArgumentException("商户ID不能为空", nameof(merchantId));

            ReviewStatus = InterchangeReviewStatus.Pending;
            ProposedByMerchantId = merchantId;
            ReviewComment = null;
            UpdateTimestamp();
        }

        /// <summary>平台审核通过（v2.16.0）：生效并清空驳回理由</summary>
        public void Approve()
        {
            ReviewStatus = InterchangeReviewStatus.Approved;
            ReviewComment = null;
            UpdateTimestamp();
        }

        /// <summary>平台驳回（v2.16.0）：记录理由，提交方可改后重提</summary>
        public void Reject(string reason)
        {
            ReviewStatus = InterchangeReviewStatus.Rejected;
            ReviewComment = reason;
            UpdateTimestamp();
        }

        /// <summary>
        /// 判断是否为完全替代
        /// </summary>
        public bool IsExact() => InterchangeType == "exact";

        /// <summary>
        /// 判断是否为有条件替代
        /// </summary>
        public bool IsConditional() => InterchangeType == "conditional";

        /// <summary>
        /// 判断是否为功能替代（v2.16.0：列为"仅供参考"，不进自动推荐）
        /// </summary>
        public bool IsFunctional() => InterchangeType == "functional";

        /// <summary>公域可见（v2.16.0）：未删除且已生效</summary>
        public bool IsPubliclyVisible => IsActive && ReviewStatus == InterchangeReviewStatus.Approved;

        /// <summary>是否可进自动推荐（v2.16.0）：公域可见且非"仅供参考"档</summary>
        public bool IsAutoRecommendable => IsPubliclyVisible && !IsFunctional();

        // 改动说明（v2.13.2 僵尸方法清理）：删除无任何调用方的 GetConfidenceLevel 与
        //   GetInterchangeTypeDescription——后者还与唯一口径 BearingInterchangeExtensions.ToTypeLabel 分歧，
        //   是"同一概念两套标签"的漂移源；标签统一由扩展方法提供
    }
}
