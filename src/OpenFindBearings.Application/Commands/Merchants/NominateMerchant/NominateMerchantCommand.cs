using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Merchants.NominateMerchant
{
    /// <summary>
    /// 提名他人为管理员命令（入驻模式 B）
    /// 发起人填被提名人手机号/邮箱 + 商户草稿信息，建 Draft 商户 + Nomination 邀请；
    /// 被提名人接受并补资料后转为 Pending，审核通过时建双方成员行
    /// </summary>
    public record NominateMerchantCommand : IRequest<string>, ICommand
    {
        /// <summary>
        /// 发起人业务用户ID
        /// </summary>
        public Guid InitiatorUserId { get; init; }

        /// <summary>
        /// 被提名人手机号（与邮箱二选一）
        /// </summary>
        public string? NomineePhone { get; init; }

        /// <summary>
        /// 被提名人邮箱（与手机号二选一）
        /// </summary>
        public string? NomineeEmail { get; init; }

        /// <summary>
        /// 商户名称
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        /// 商家类型（MerchantType 枚举值）
        /// </summary>
        public int? Type { get; init; }

        /// <summary>
        /// 公司全称
        /// </summary>
        public string? CompanyName { get; init; }

        /// <summary>
        /// 联系人
        /// </summary>
        public string? ContactPerson { get; init; }

        /// <summary>
        /// 固定电话
        /// </summary>
        public string? Phone { get; init; }

        /// <summary>
        /// 手机号
        /// </summary>
        public string? Mobile { get; init; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public string? Email { get; init; }

        /// <summary>
        /// 地址
        /// </summary>
        public string? Address { get; init; }

        /// <summary>
        /// 发起人是否默认入伙为员工（审核通过时生效）
        /// </summary>
        public bool InitiatorJoins { get; init; } = true;
    }
}
