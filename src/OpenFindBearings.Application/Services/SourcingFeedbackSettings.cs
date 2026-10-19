namespace OpenFindBearings.Application.Services
{
    /// <summary>
    /// 成交回执反刷规则配置键与默认值（v2.18.0 撮合成交链）：
    /// 阈值走 SystemConfig（Business.* 键，Admin 可调实时生效），用于判定一条回执是否计分。
    /// </summary>
    public static class SourcingFeedbackSettings
    {
        /// <summary>需求方账号最短注册天数（防批量小号刷分；0=不校验）</summary>
        public const string MinAccountAgeDaysKey = "Business.SourcingFeedbackMinAccountAgeDays";
        /// <summary>同 IP 异常检测的时间窗（小时）</summary>
        public const string IpWindowHoursKey = "Business.SourcingFeedbackIpWindowHours";
        /// <summary>同 IP 时间窗内允许的计分回执上限（超出视为可疑不计分）</summary>
        public const string IpMaxCountKey = "Business.SourcingFeedbackIpMax";

        /// <summary>需求方账号最短注册天数默认值</summary>
        public const int MinAccountAgeDaysDefault = 1;
        /// <summary>同 IP 异常检测时间窗默认值（小时）</summary>
        public const int IpWindowHoursDefault = 24;
        /// <summary>同 IP 时间窗内计分回执上限默认值</summary>
        public const int IpMaxCountDefault = 5;
    }
}
