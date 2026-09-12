using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Merchants.AcceptNomination
{
    /// <summary>
    /// 接受管理员提名命令（入驻模式 B 被提名人侧）
    /// 被提名人接受邀请并补全商户资料，Draft 商户转为 Pending 等待 Admin 审核
    /// </summary>
    public record AcceptNominationCommand : IRequest<Guid>, ICommand
    {
        /// <summary>
        /// 提名邀请码
        /// </summary>
        public string InvitationCode { get; init; } = string.Empty;

        /// <summary>
        /// 被提名人业务用户ID（当前登录用户）
        /// </summary>
        public Guid NomineeUserId { get; init; }

        /// <summary>
        /// 被提名人手机号（取自 JWT phone_number claim，与邀请手机号比对防冒领）
        /// </summary>
        public string? NomineePhone { get; init; }

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
        /// 公司全称
        /// </summary>
        public string? CompanyName { get; init; }

        /// <summary>
        /// 统一社会信用代码
        /// </summary>
        public string? UnifiedSocialCreditCode { get; init; }

        /// <summary>
        /// 商家简介
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// 营业执照图片 URL（可选，用于后续认证）
        /// </summary>
        public string? LicenseUrl { get; init; }
    }
}
