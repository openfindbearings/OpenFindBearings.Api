using MediatR;

namespace OpenFindBearings.Domain.Events
{
    /// <summary>
    /// 轴承创建事件
    /// 当新轴承添加到系统时触发
    /// </summary>
    public class BearingCreatedEvent : INotification
    {
        /// <summary>
        /// 轴承ID
        /// </summary>
        public Guid BearingId { get; }

        /// <summary>
        /// 轴承型号
        /// </summary>
        public string PartNumber { get; }

        /// <summary>
        /// 品牌ID
        /// </summary>
        public Guid BrandId { get; }

        /// <summary>
        /// 事件发生时间
        /// </summary>
        public DateTime OccurredOn { get; }

        /// <summary>
        /// 创建轴承创建事件
        /// </summary>
        public BearingCreatedEvent(Guid bearingId, string partNumber, Guid brandId)
        {
            BearingId = bearingId;
            PartNumber = partNumber;
            BrandId = brandId;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 轴承更新事件
    /// 当轴承信息被修改时触发
    /// </summary>
    public class BearingUpdatedEvent : INotification
    {
        /// <summary>
        /// 轴承ID
        /// </summary>
        public Guid BearingId { get; }

        /// <summary>
        /// 轴承型号
        /// </summary>
        public string PartNumber { get; }

        /// <summary>
        /// 被修改的字段列表
        /// </summary>
        public List<string> ChangedFields { get; }

        /// <summary>
        /// 事件发生时间
        /// </summary>
        public DateTime OccurredOn { get; }

        /// <summary>
        /// 创建轴承更新事件
        /// </summary>
        public BearingUpdatedEvent(Guid bearingId, string partNumber, List<string> changedFields)
        {
            BearingId = bearingId;
            PartNumber = partNumber;
            ChangedFields = changedFields;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 轴承被查看事件
    /// 用于统计热门产品、用户行为分析
    /// </summary>
    public class BearingViewedEvent : INotification
    {
        /// <summary>
        /// 轴承ID
        /// </summary>
        public Guid BearingId { get; }

        /// <summary>
        /// 轴承型号
        /// </summary>
        public string PartNumber { get; }

        /// <summary>
        /// 查看的用户ID（如果是登录用户）
        /// </summary>
        public Guid? UserId { get; }

        /// <summary>
        /// 游客会话ID（如果是未登录用户）
        /// </summary>
        public string? SessionId { get; }

        /// <summary>
        /// 当前浏览次数
        /// </summary>
        public int CurrentViewCount { get; }

        /// <summary>
        /// 查看时间
        /// </summary>
        public DateTime ViewedAt { get; }

        /// <summary>
        /// 创建轴承被查看事件
        /// </summary>
        public BearingViewedEvent(
            Guid bearingId,
            string partNumber,
            Guid? userId = null,
            string? sessionId = null,
            int currentViewCount = 0)
        {
            BearingId = bearingId;
            PartNumber = partNumber;
            UserId = userId;
            SessionId = sessionId;
            CurrentViewCount = currentViewCount;
            ViewedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 是否为登录用户查看
        /// </summary>
        public bool IsAuthenticated => UserId.HasValue;

        /// <summary>
        /// 是否为游客查看
        /// </summary>
        public bool IsGuest => !UserId.HasValue && !string.IsNullOrEmpty(SessionId);
    }

    /// <summary>
    /// 轴承下架事件
    /// 当商家下架某轴承时触发
    /// </summary>
    public class BearingTakenOffShelfEvent : INotification
    {
        /// <summary>
        /// 商家-轴承关联ID
        /// </summary>
        public Guid MerchantBearingId { get; }

        /// <summary>
        /// 商家ID
        /// </summary>
        public Guid MerchantId { get; }

        /// <summary>
        /// 轴承ID
        /// </summary>
        public Guid BearingId { get; }

        /// <summary>
        /// 事件发生时间
        /// </summary>
        public DateTime OccurredOn { get; }

        public BearingTakenOffShelfEvent(Guid merchantBearingId, Guid merchantId, Guid bearingId)
        {
            MerchantBearingId = merchantBearingId;
            MerchantId = merchantId;
            BearingId = bearingId;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 轴承上架事件
    /// 当商家重新上架某轴承时触发
    /// </summary>
    public class BearingPutOnShelfEvent : INotification
    {
        public Guid MerchantBearingId { get; }
        public Guid MerchantId { get; }
        public Guid BearingId { get; }
        public DateTime OccurredOn { get; }

        public BearingPutOnShelfEvent(Guid merchantBearingId, Guid merchantId, Guid bearingId)
        {
            MerchantBearingId = merchantBearingId;
            MerchantId = merchantId;
            BearingId = bearingId;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 轴承提交审核事件
    /// 当商家添加新产品提交审核时触发
    /// </summary>
    public class BearingSubmittedForApprovalEvent : INotification
    {
        public Guid MerchantBearingId { get; }
        public Guid MerchantId { get; }
        public Guid BearingId { get; }
        public DateTime OccurredOn { get; }

        public BearingSubmittedForApprovalEvent(Guid merchantBearingId, Guid merchantId, Guid bearingId)
        {
            MerchantBearingId = merchantBearingId;
            MerchantId = merchantId;
            BearingId = bearingId;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 轴承审核通过事件
    /// 当管理员审核通过商家的产品时触发
    /// </summary>
    public class BearingApprovedEvent : INotification
    {
        public Guid MerchantBearingId { get; }
        public Guid MerchantId { get; }
        public Guid BearingId { get; }
        public DateTime OccurredOn { get; }

        public BearingApprovedEvent(Guid merchantBearingId, Guid merchantId, Guid bearingId)
        {
            MerchantBearingId = merchantBearingId;
            MerchantId = merchantId;
            BearingId = bearingId;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 轴承审核拒绝事件
    /// 当管理员拒绝商家的产品时触发
    /// </summary>
    public class BearingRejectedEvent : INotification
    {
        public Guid MerchantBearingId { get; }
        public Guid MerchantId { get; }
        public Guid BearingId { get; }
        public string RejectReason { get; }
        public DateTime OccurredOn { get; }

        public BearingRejectedEvent(Guid merchantBearingId, Guid merchantId, Guid bearingId, string rejectReason)
        {
            MerchantBearingId = merchantBearingId;
            MerchantId = merchantId;
            BearingId = bearingId;
            RejectReason = rejectReason;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 轴承被收藏事件
    /// 当用户收藏轴承时触发
    /// </summary>
    public class BearingFavoritedEvent : INotification
    {
        public Guid UserId { get; }
        public Guid BearingId { get; }
        public DateTime OccurredOn { get; }

        public BearingFavoritedEvent(Guid userId, Guid bearingId)
        {
            UserId = userId;
            BearingId = bearingId;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 轴承被取消收藏事件
    /// 当用户取消收藏轴承时触发
    /// </summary>
    public class BearingUnfavoritedEvent : INotification
    {
        public Guid UserId { get; }
        public Guid BearingId { get; }
        public DateTime OccurredOn { get; }

        public BearingUnfavoritedEvent(Guid userId, Guid bearingId)
        {
            UserId = userId;
            BearingId = bearingId;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
