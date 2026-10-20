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
        /// 商家类型（枚举名：Manufacturer/AuthorizedDealer/Distributor/Trader）
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>类型中文展示名（生产厂家/授权经销商/分销商/贸易商）。
        /// 改动说明（v2.13.2）：与 GradeDisplay 对齐补齐——原只有 Type（枚举名），
        /// 消费方各自硬编码枚举→中文映射，且 ClaimableMerchantDto 同名 Type 却存中文造成跨 DTO 语义漂移</summary>
        public string TypeDisplay { get; set; } = string.Empty;

        /// <summary>
        /// 联系人姓名
        /// </summary>
        public string? ContactPerson { get; set; }

        // 改动说明（v1.6.0 电话单轨）：移除 Phone/Mobile/Email——联系方式只在寻货撮合达成后互给，
        //   公共查询面一律不下发（原靠 isAuthenticated 屏蔽，现整体移除，杜绝绕过）

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

    /// <summary>等级中文展示名（入驻/认证/口碑/金牌，v2.5.0 商家经济；v2.13.0 Lv3 改名口碑商家）</summary>
    public string GradeDisplay { get; set; } = string.Empty;

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
        /// 入驻生效时间（UTC，审核通过时写入）。
        /// 改动说明（v1.50.0 入驻统计）：null=尚未入驻（互联网/种子/未认领数据）；用于区分互联网新增与入驻新增。
        /// </summary>
        public DateTime? OnboardedAt { get; set; }

        /// <summary>
        /// 审核拒绝原因（Suspended 时有值）。
        /// 改动说明：审批页"已拒绝"Tab 与抽屉展示驳回原因，此前只能从实体列间接推断
        /// </summary>
        public string? RejectReason { get; set; }

        /// <summary>
        /// 数据来源（Internet/Manual，null=未知）。
        /// 改动说明（v1.31.0）：Admin 商户列表"解除归属"按钮可用性判定需要——
        /// 提名已有商户 ApplicationMode=None 但 DataSource=Manual，与公海商户（None+Internet）
        /// 同渠道不同语义，仅靠渠道字段无法区分，与 DetachMerchant 守卫严格同口径
        /// </summary>
        public string? DataSource { get; set; }

        /// <summary>
        /// 牌级有效期起（UTC，v2.19.0 有效期制）。
        /// 改动说明：Admin 牌级页展示"有效期至"与到期降档依据，仅管理端下发
        /// </summary>
        public DateTime? GradePeriodStartAt { get; set; }

        /// <summary>
        /// 牌级有效期止（UTC，v2.19.0 有效期制）；普通档为空
        /// </summary>
        public DateTime? GradePeriodEndAt { get; set; }

        /// <summary>
        /// 在售不足保级截止（UTC，v2.12.0 等级玩法）；为空表示当前在售达标或无缓冲
        /// </summary>
        public DateTime? GradeGraceUntil { get; set; }

        /// <summary>
        /// 公域可匹配在售件数（v2.19.0 审计修复，仅管理端下发）。
        /// 改动说明：Admin 牌级页原以"当前牌级 &gt; 普通"推断在售达标，牌级未及时重算时会误报达标；
        ///   改为下发实时件数与阈值，由页面直接显示"在售 N/M"并判定。
        /// </summary>
        public int? OnSaleCount { get; set; }

        /// <summary>在售达标阈值（v2.19.0，仅管理端下发，取 SystemConfig 实时值）</summary>
        public int? OnSaleMin { get; set; }
    }
}
