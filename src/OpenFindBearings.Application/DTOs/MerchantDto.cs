namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 商家列表项DTO
    /// 用于商家搜索列表、关注列表等场景
    /// </summary>
    public class MerchantDto
    {
        /// <summary>
        /// 商家ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 商家名称（展示用）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 公司全称
        /// </summary>
        public string? CompanyName { get; set; }

        /// <summary>
        /// 商家类型
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 联系人姓名
        /// </summary>
        public string? ContactPerson { get; set; }

        /// <summary>
        /// 固定电话
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// 手机号码
        /// </summary>
        public string? Mobile { get; set; }

        /// <summary>
        /// 电子邮箱
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// 是否已认证
        /// </summary>
        public bool IsVerified { get; set; }

        /// <summary>
        /// 商户是否已主动申请认证（v2.9.0：BFF application 透传给 Taro 按钮态；Admin 认证后清除）
        /// </summary>
        public bool VerifyRequested { get; set; }

        /// <summary>
        /// 商家状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 商家等级
        /// </summary>
        public string Grade { get; set; } = string.Empty;

        /// <summary>
        /// 粉丝数量
        /// </summary>
        public int FollowerCount { get; set; }

        /// <summary>
        /// 产品数量
        /// </summary>
        public int ProductCount { get; set; }

        /// <summary>
        /// 商家Logo（如果有）
        /// </summary>
        public string? LogoUrl { get; set; }

        /// <summary>
        /// 入驻渠道（None/Self/Claim/Nomination）。
        /// 改动说明：Admin 审批抽屉需区分自助/认领/提名路径展示
        /// </summary>
        public string ApplicationMode { get; set; } = string.Empty;

        /// <summary>
        /// 申请提交时间（UTC，取实体 CreatedAt）。
        /// 改动说明：审批页按提交时间排序与 SLA 展示用
        /// </summary>
        public DateTime SubmittedAt { get; set; }

        /// <summary>
        /// 审核拒绝原因（Suspended 时有值）。
        /// 改动说明：审批页"已拒绝"Tab 与抽屉展示驳回原因，此前只能从实体列间接推断
        /// </summary>
        public string? RejectReason { get; set; }
    }
}
