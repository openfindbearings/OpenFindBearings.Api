using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Merchants.ApplyMerchant
{
    /// <summary>
    /// 商户入驻申请命令（模式 self 自助当管理员 / claim 认领爬虫商家）
    /// 提交后创建/绑定 Pending 商户 + 申请人 MerchantAdmin 成员行，待 Admin 审核
    /// </summary>
    public record ApplyMerchantCommand : IRequest<Guid>, ICommand
    {
        /// <summary>
        /// 申请模式（self / claim）
        /// </summary>
        public string Mode { get; init; } = "self";

        /// <summary>
        /// 认领的已有爬虫商家ID（mode=claim 时必填）
        /// </summary>
        public Guid? ClaimMerchantId { get; init; }

        /// <summary>
        /// 商户名称（mode=self 时必填）
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        /// 商家类型（MerchantType 枚举值，mode=self 时必填）
        /// </summary>
        public int? Type { get; init; }

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

        /// <summary>
        /// 申请人业务用户ID
        /// </summary>
        public Guid ApplicantUserId { get; init; }
    }
}
