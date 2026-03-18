using MediatR;
using OpenFindBearings.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFindBearings.Domain.Events
{
    /// <summary>
    /// 商家认证通过事件
    /// 当管理员认证商家时触发
    /// </summary>
    public class MerchantVerifiedEvent : INotification
    {
        public Guid MerchantId { get; }
        public string MerchantName { get; }
        public DateTime OccurredOn { get; }

        public MerchantVerifiedEvent(Guid merchantId, string merchantName)
        {
            MerchantId = merchantId;
            MerchantName = merchantName;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 商家取消认证事件
    /// 当管理员取消商家认证时触发
    /// </summary>
    public class MerchantUnverifiedEvent : INotification
    {
        public Guid MerchantId { get; }
        public string MerchantName { get; }
        public DateTime OccurredOn { get; }

        public MerchantUnverifiedEvent(Guid merchantId, string merchantName)
        {
            MerchantId = merchantId;
            MerchantName = merchantName;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 商家等级变更事件
    /// 当商家等级变化时触发
    /// </summary>
    public class MerchantGradeChangedEvent : INotification
    {
        public Guid MerchantId { get; }
        public MerchantGrade OldGrade { get; }
        public MerchantGrade NewGrade { get; }
        public DateTime OccurredOn { get; }

        public MerchantGradeChangedEvent(Guid merchantId, MerchantGrade oldGrade, MerchantGrade newGrade)
        {
            MerchantId = merchantId;
            OldGrade = oldGrade;
            NewGrade = newGrade;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 商家被关注事件
    /// 当用户关注商家时触发
    /// </summary>
    public class MerchantFollowedEvent : INotification
    {
        public Guid UserId { get; }
        public Guid MerchantId { get; }
        public DateTime OccurredOn { get; }

        public MerchantFollowedEvent(Guid userId, Guid merchantId)
        {
            UserId = userId;
            MerchantId = merchantId;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 商家被取消关注事件
    /// 当用户取消关注商家时触发
    /// </summary>
    public class MerchantUnfollowedEvent : INotification
    {
        public Guid UserId { get; }
        public Guid MerchantId { get; }
        public DateTime OccurredOn { get; }

        public MerchantUnfollowedEvent(Guid userId, Guid merchantId)
        {
            UserId = userId;
            MerchantId = merchantId;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
