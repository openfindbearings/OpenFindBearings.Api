using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Merchants.ResubmitApplication
{
    /// <summary>
    /// 被拒后修改资料重新提交命令（v2.6.0 新增）。
    /// 守卫：商户处于 Suspended（仅被驳回的申请可重提）且调用者是其在职 MerchantAdmin（申请人本人）。
    /// 渠道：self/claim 支持；Nomination/None 拒绝（提名重提走原"接受提名"通道，历史数据无渠道不开放）。
    /// 字段级合并更新资料（未编辑项保留原值）后 Resubmit() 回 Pending 重新进审核队列。
    /// </summary>
    public record ResubmitApplicationCommand : IRequest, ICommand
    {
        /// <summary>
        /// 被驳回的入驻申请所属商户ID
        /// </summary>
        public Guid MerchantId { get; init; }

        /// <summary>
        /// 调用者（申请人）用户ID
        /// </summary>
        public Guid ApplicantUserId { get; init; }

        /// <summary>商家名称（必填）</summary>
        public string? Name { get; init; }

        /// <summary>商家类型（MerchantType 枚举值，null 表示不改）</summary>
        public int? Type { get; init; }

        /// <summary>对外联系人</summary>
        public string? ContactPerson { get; init; }

        /// <summary>客服电话（对外公开）</summary>
        public string? Phone { get; init; }

        /// <summary>手机</summary>
        public string? Mobile { get; init; }

        /// <summary>邮箱</summary>
        public string? Email { get; init; }

        /// <summary>地址</summary>
        public string? Address { get; init; }

        /// <summary>营业执照企业名称（必填）</summary>
        public string? CompanyName { get; init; }

        /// <summary>统一社会信用代码</summary>
        public string? UnifiedSocialCreditCode { get; init; }

        /// <summary>商户简介</summary>
        public string? Description { get; init; }

        /// <summary>可选随附营业执照图片 URL（追加一条待审记录，不影响入驻状态）</summary>
        public string? LicenseUrl { get; init; }
    }
}
